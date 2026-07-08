using darsakApi;
using darsakApi.Data;
using darsakApi.Models;
using darsakApi.utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace darsakApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController(AppDbContext context, AuthService authService, IWebHostEnvironment env) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly AuthService _authService = authService;
        private readonly IWebHostEnvironment _env = env;

        [HttpPost("auth/student")]
        public async Task<IActionResult> SignInOrSignUp([FromForm] Student request)



        {
            var existingStudent = await _context.Students.Include(s => s.StudentStage).FirstOrDefaultAsync(u => u.Email == request.Email);


            if (existingStudent != null)
            {
                var token = _authService.GenerateJwtToken(existingStudent.Email, "student");
                return Ok(new
                {
                    message = "أهلا بك مجددا",
                    success = true,
                    student = existingStudent,
                    token
                });
            }
            else
            {
                if (!ModelState.IsValid &&
               ModelState.ContainsKey(nameof(request.Email)) &&
               ModelState[nameof(request.Email)]!.Errors.Count > 0)
                {
                    return _authService.ValidationError(ModelState); // BadRequest();
                }




                // 3. New student — run full model validation ([Required], [RegularExpression], etc.)
                ModelState.Clear();
                if (!TryValidateModel(request))
                    return _authService.ValidationError(ModelState); // BadRequest();

                // 4. Look up all stages matching the sent StageName
                var matchingStages = await _context.StudentStages
                    .Where(ss => ss.Stage == request.StageName)
                    .ToListAsync();

                // 4a. Stage name doesn't exist at all
                if (matchingStages.Count == 0)
                    return BadRequest(new { message = "المرحلة الدراسية غير موجودة." });

                // 4b. Find the specific stage that matches StageName + Department
                var matchedStage = matchingStages.FirstOrDefault(ss => ss.Department == request.Department);

                if (matchedStage == null)
                {
                    // Return valid departments for this stage name to help the client
                    var validDepartments = matchingStages.Select(ss => ss.Department).ToArray();
                    return BadRequest(new
                    {
                        message = $"القسم '{request.Department}' غير صحيح لمرحلة '{request.StageName}'.",
                        validDepartments
                    });
                }


                // 4c. Valid — set the FK and save
                request.StudentStageId = matchedStage.Id;

                _context.Students.Add(request);
                await _context.SaveChangesAsync();

                var newToken = _authService.GenerateJwtToken(request.Email, "student");
                return Ok(new
                {
                    message = "تم تسجيل الدخول بنجاح",
                    success = true,
                    student = request,
                    token = newToken
                });

            }






        }















        // ─────────────────────────────────────────────────────────────
        // POST api/auth/teacher
        // Content-Type: multipart/form-data
        //
        // Sign-in  → send Email only (returns token immediately)
        // Sign-up  → send all teacher fields + certifications[] images
        // ─────────────────────────────────────────────────────────────
        [HttpPost("auth/teacher")]
        public async Task<IActionResult> TeacherSignInOrSignUp(
            [FromForm] Teacher request,
            [FromForm] List<IFormFile>? certifications)
        {
            // 1. Always validate email first
            if (!ModelState.IsValid &&
                ModelState.ContainsKey(nameof(request.Email)) &&
                ModelState[nameof(request.Email)]!.Errors.Count > 0)
            {
                return _authService.ValidationError(ModelState); // BadRequest();
            }

            // 2. Existing teacher → sign in (no certifications needed)
            var existingTeacher = await _context.Teachers
                .Include(t => t.Certifications)
                .FirstOrDefaultAsync(t => t.Email == request.Email);

            if (existingTeacher != null)
            {
                var token = _authService.GenerateJwtToken(existingTeacher.Email, "teacher");
                return Ok(new
                {
                    message = "أهلا بك مجددا",
                    success = true,
                    teacher = existingTeacher,
                    token
                });
            }

            // 3. New teacher — run full model validation
            ModelState.Clear();
            if (!TryValidateModel(request))
                return _authService.ValidationError(ModelState); // BadRequest();

            // 4. At least one certification image is required for new sign-up
            if (certifications == null || certifications.Count == 0)
                return BadRequest(new { message = "يجب إرفاق صورة شهادة واحدة على الأقل عند التسجيل." });

            // 5. Validate each image file
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var allowedMime = new[] { "image/jpeg", "image/png", "image/webp" };
            const long maxBytes = 10 * 1024 * 1024; // 10 MB

            foreach (var img in certifications)
            {
                var ext = Path.GetExtension(img.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext) || !allowedMime.Contains(img.ContentType))
                    return BadRequest(new { message = $"الملف '{img.FileName}' غير مدعوم. يُسمح فقط بـ JPG أو PNG أو WebP." });

                if (img.Length > maxBytes)
                    return BadRequest(new { message = $"حجم الملف '{img.FileName}' يتجاوز الحد المسموح به (10 ميغابايت)." });
            }

            // 6. Save teacher record (status = pending by default)
            _context.Teachers.Add(request);
            await _context.SaveChangesAsync(); // request.Id is now populated

            // 7. Save each certification image to disk + DB
            // WebRootPath is null when wwwroot/ doesn't exist — fall back and create it
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "certifications");
            Directory.CreateDirectory(uploadsFolder);

            var savedCerts = new List<object>();

            for (int i = 0; i < certifications.Count; i++)
            {
                var img = certifications[i];
                var ext = Path.GetExtension(img.FileName).ToLowerInvariant();
                var uniqueName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, uniqueName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                    await img.CopyToAsync(stream);

                var relativePath = $"uploads/certifications/{uniqueName}";

                var cert = new TeacherCertification
                {
                    TeacherId = request.Id,
                    ImagePath = $"{Request.Scheme}://{Request.Host}/{relativePath}"
                };

                _context.TeacherCertifications.Add(cert);
                savedCerts.Add(new
                {
                    cert.ImagePath,
                    relativePath
                });
            }

            await _context.SaveChangesAsync();

            var newToken = _authService.GenerateJwtToken(request.Email, "teacher");
            return Ok(new
            {
                message = "تم تسجيل المعلم بنجاح. في انتظار المراجعة والموافقة.",
                isSignedUp = false,
                teacher = new
                {
                    request.Id,
                    request.FullName,
                    request.Email,
                    request.Phone,
                    request.Gender,
                    request.Government,
                    request.NationalId,
                    request.StageKind,
                    request.StudentSubCount,
                    request.MaterialCount,
                    request.LessonCount,
                    request.RatingCount,
                    request.Status,
                },
                certifications = savedCerts,
                token = newToken
            });
        }

        // private BadRequestObjectResult ValidationError()
        // {
        //     var errors = ModelState
        //         .Where(e => e.Value?.Errors.Count > 0)
        //         .SelectMany(e => e.Value!.Errors.Select(er => er.ErrorMessage))
        //         .ToArray();

        //     return BadRequest(new
        //     {
        //         message = "البيانات المرسلة غير صحيحة، يرجى التحقق من الحقول.",
        //         errors
        //     });
        // }

    }
}
