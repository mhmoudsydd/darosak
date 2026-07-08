using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using darsakApi.Data;
using darsakApi.Models;
using darsakApi.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace darsakApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class TeacherController(AppDbContext context, IWebHostEnvironment env, AuthService authService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly AuthService _authService = authService;    

        // ────────────────────────────────────────────────────────────────
        // GET api/teachers
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            var teachers = await _context.Teachers
                .Include(m => m.TeacherMaterials)
                .ThenInclude(tm => tm.Material)
                .ThenInclude(m => m.StudentStage)
                .Include(t => t.Certifications)
                .ToListAsync();

            return Ok(new
            {
                message = "تم جلب بيانات المعلمين بنجاح",
                total = teachers.Count,
                teachers
            });
        }

        // ────────────────────────────────────────────────────────────────
        // GET api/teachers/by-stage/{stageId}
        // ────────────────────────────────────────────────────────────────
        [HttpGet("teachers/by-stage/{stageId}")]
        public async Task<ActionResult<IEnumerable<Teacher>>> GetTeachersByStage(int stageId)
        {
            return await _context.Teachers
                .Include(t => t.TeacherMaterials)
                    .ThenInclude(tm => tm.Material)
                        .ThenInclude(m => m.StudentStage)
                .Where(t => t.TeacherMaterials.Any(tm => tm.Material.StudentStageId == stageId))
                .ToListAsync();
        }

        // ────────────────────────────────────────────────────────────────
        // GET api/teachers/{id}
        // Returns teacher details including certifications
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpGet("teachers/{id}")]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Certifications)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
                return NotFound(new { message = "المعلم غير موجود." });

            var certifications = teacher.Certifications.Select(c => new
            {
                c.Id,
                c.Title,
                c.ImagePath,
                imageUrl = $"{Request.Scheme}://{Request.Host}/{c.ImagePath}"
            });

            return Ok(new
            {
                message = "تم جلب بيانات المعلم بنجاح",
                teacher = new
                {
                    teacher.Id,
                    teacher.FullName,
                    teacher.Email,
                    teacher.Phone,
                    teacher.NationalId,
                    teacher.Gender,
                    teacher.Government,
                    teacher.StageKind,
                    teacher.Status,
                    certifications
                }
            });
        }

        // ────────────────────────────────────────────────────────────────────
        // POST api/teachers/{id}/certifications
        // Add more certification images to an existing teacher account
        // ────────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpPost("teachers/{id}/certifications")]
        public async Task<IActionResult> AddCertifications(
            int id,
            [FromForm] List<IFormFile> certifications,
            [FromForm] List<string>? certificationTitles)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
                return NotFound(new { message = "المعلم غير موجود." });

            if (certifications == null || certifications.Count == 0)
                return BadRequest(new { message = "لم يتم إرسال أي صور." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            const long maxImageBytes = 5 * 1024 * 1024;

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "certifications");
            Directory.CreateDirectory(uploadsFolder);

            var saved = new List<object>();

            for (int i = 0; i < certifications.Count; i++)
            {
                var img = certifications[i];
                var ext = Path.GetExtension(img.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext) || !allowedContentTypes.Contains(img.ContentType))
                    return BadRequest(new { message = $"الملف '{img.FileName}' غير مدعوم." });

                if (img.Length > maxImageBytes)
                    return BadRequest(new { message = $"حجم الملف '{img.FileName}' يتجاوز 5 ميغابايت." });

                var uniqueName = $"{Guid.NewGuid()}{ext}";
                var fullPath = Path.Combine(uploadsFolder, uniqueName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                    await img.CopyToAsync(stream);

                var relativePath = $"uploads/certifications/{uniqueName}";
                var title = certificationTitles != null && i < certificationTitles.Count
                    ? certificationTitles[i]
                    : null;

                var cert = new TeacherCertification
                {
                    TeacherId = id,
                    ImagePath = relativePath,
                    Title = title
                };

                _context.TeacherCertifications.Add(cert);
                saved.Add(new
                {
                    cert.Title,
                    cert.ImagePath,
                    imageUrl = $"{Request.Scheme}://{Request.Host}/{relativePath}"
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم رفع الشهادات بنجاح.",
                certifications = saved
            });
        }







        [Authorize(Roles = "teacher")]
        [HttpGet("teachers/{teacherId}/posts")]
        public async Task<IActionResult> GetTeacherPosts(int teacherId)
        {
            var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == teacherId);
            if (!teacherExists)
                return NotFound(new { message = "المعلم غير موجود." });

            var posts = await _context.Posts
                .Where(p => p.TeacherId == teacherId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            if (posts.Count == 0)
                return NotFound(new { message = "المعلم لا يمتلك منشورات." });

            return Ok(new
            {
                message = "تم جلب المنشورات بنجاح.",
                total = posts.Count,
                posts = posts.Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    tag = p.Tag.ToString(),
                    createdAt = ImpFunction.ToArabicNumbers(p.CreatedAt.ToString("yyyy/MM/dd"))
                })
            });
        }


        [Authorize(Roles = "teacher")]

        [HttpGet("Teachers/{teacherId}/materials")]
        public async Task<IActionResult> GetTeacherMaterial(int teacherId)
        {
            var teacherExists = await _context.Teachers.AnyAsync(t => t.Id == teacherId);
            if (!teacherExists)
                return NotFound(new { message = "المعلم غير موجود." });

            var materials = await _context.Materials
            .Where(m => m.TeacherMaterials.Any(tm => tm.TeacherId == teacherId))
            .ToListAsync();
            if (materials.Count == 0)
                return NotFound(new { message = "المعلم لا يمتلك مواد." });

            return Ok(new
            {
                message = "تم جلب المواد بنجاح.",
                total = materials.Count,
                materials = materials.Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Description,
                    m.CreatedAt,
                    studentStage = m.StudentStage == null ? null : new
                    {
                        m.StudentStage.Id,
                        m.StudentStage.Stage,
                        m.StudentStage.Department
                    },

                })
            });
        }

        [Authorize(Roles = "teacher")]
        [HttpGet("teachers/subscriptions")]
        public async Task<IActionResult> GetTeacherSubscriptions([FromQuery] string? status)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });

            var query = _context.MaterialSubscriptions
                .Include(ms => ms.Student).ThenInclude(s => s.StudentStage)
                .Include(ms => ms.Material)
                .Where(ms => ms.TeacherId == teacher.Id);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(ms => ms.Status == status);
            }

            var subscriptions = await query
                .OrderByDescending(ms => ms.SubscribedAt)
                .Select(ms => new
                {
                    ms.Id,
                    ms.Price,
                    ms.Status,
                    ms.SubscribedAt,
                    material = new
                    {
                        ms.Material.Id,
                        ms.Material.Name,
                        ms.Material.Description
                    },
                    student = new
                    {
                        ms.Student.Id,
                        ms.Student.FullName,
                        ms.Student.Email,
                        ms.Student.StudentPhone,
                        ms.Student.FatherPhone,
                        ms.Student.Gender,
                        ms.Student.Government,
                        stage = ms.Student.StudentStage == null ? null : new
                        {
                            ms.Student.StudentStage.Id,
                            ms.Student.StudentStage.Stage,
                            ms.Student.StudentStage.Department
                        }
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                message = "تم جلب طلبات الاشتراك بنجاح",
                total = subscriptions.Count,
                subscriptions
            });
        }

        public class UpdateSubscriptionStatusRequest
        {
            [Required(ErrorMessage = "حقل الحالة مطلوب.")]
            [RegularExpression("^(مقبول|مرفوض)$", ErrorMessage = "يجب أن تكون الحالة 'مقبول' أو 'مرفوض'.")]
            public string Status { get; set; } = string.Empty;
        }

        [Authorize(Roles = "teacher")]
        [HttpPut("teachers/subscriptions/{subscriptionId}/status")]
        public async Task<IActionResult> UpdateSubscriptionStatus(int subscriptionId, [FromForm] UpdateSubscriptionStatusRequest request)
        {
             if (!ModelState.IsValid)
                return _authService.ValidationError(ModelState);

            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });

            var subscription = await _context.MaterialSubscriptions
                .FirstOrDefaultAsync(ms => ms.Id == subscriptionId && ms.TeacherId == teacher.Id);

            if (subscription == null)
                return NotFound(new { message = "طلب الاشتراك غير موجود أو لا ينتمي إليك." });

            var oldStatus = subscription.Status;
            var newStatus = request.Status;

            if (oldStatus == newStatus)
            {
                return Ok(new { message = "حالة الاشتراك هي بالفعل " + newStatus, subscription });
            }

            // Update status
            subscription.Status = newStatus;

            // Handle Teacher's StudentSubCount counter
            if (newStatus == "Approved" && oldStatus != "Approved")
            {
                teacher.StudentSubCount += 1;
            }
            else if (oldStatus == "Approved" && newStatus != "Approved")
            {
                teacher.StudentSubCount = Math.Max(0, teacher.StudentSubCount - 1);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم تحديث حالة الاشتراك بنجاح.",
                subscription = new
                {
                    subscription.Id,
                    subscription.Price,
                    subscription.Status,
                    subscription.SubscribedAt
                }
            });
        }
    }
}
