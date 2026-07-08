using darsakApi.Data;
using darsakApi.Models;
using darsakApi.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace darsakApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class MaterialController(AppDbContext context, AuthService authService) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly AuthService _authService = authService;


        // ────────────────────────────────────────────────────────────────
        // POST api/materials
        // Requires: Bearer token with role = "teacher"
        //
        // Body (form-data):
        //   Name           string   required
        //   Description    string   required
        //   StageName      string   required  – e.g. "الصف الثالث الثانوي"
        //   Department     string   required  – e.g. "علمي"
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpPost("createMaterials")]
        public async Task<IActionResult> CreateMaterial([FromForm] Material request)
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

            // 3. Look up the StudentStage by StageName + Department
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

            // 4. Create and save the material
            request.StudentStageId = matchedStage.Id;
            request.CreatedAt = DateOnly.FromDateTime(DateTime.Now);

            _context.Materials.Add(request);
            await _context.SaveChangesAsync(); // request.Id is now populated

            // 5. Link the teacher to the new material via junction table
            _context.TeacherMaterials.Add(new TeacherMaterial
            {
                TeacherId = teacher.Id,
                MaterialId = request.Id
            });

            // 6. Increment the teacher's material counter
            teacher.MaterialCount += 1;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم إنشاء المادة بنجاح",
                material = new
                {
                    request.Id,
                    request.Name,
                    request.Description,
                    request.Price,
                    request.CreatedAt,
                    stage = new
                    {
                        matchedStage.Id,
                        matchedStage.Stage,
                        matchedStage.Department
                    },
                    teacher = new
                    {
                        teacher.Id,
                        teacher.FullName,
                        teacher.Email
                    }
                }
            });
        }




        [AllowAnonymous]
        [HttpGet("Materials")]
        public async Task<IActionResult> Materials()
        {
            var materials = await _context.Materials
                .Include(m => m.Exams).ThenInclude(e => e.Questions).ThenInclude(q => q.Choices)
                .Include(m => m.Lessons)
                .Include(m => m.MaterialSources)
                .Include(m => m.TeacherMaterials).ThenInclude(tm => tm.Teacher).ThenInclude(t => t.Certifications)
                .Include(m => m.StudentStage)
                .ToListAsync();

            var result = materials.Select(m => new
            {
                m.Id,
                m.Name,
                m.Description,
                m.Price,
                m.CreatedAt,
                studentStage = m.StudentStage == null ? null : new
                {
                    m.StudentStage.Id,
                    m.StudentStage.Stage,
                    m.StudentStage.Department
                },
                teachers = m.TeacherMaterials.Select(tm => new
                {
                    tm.Teacher.Id,
                    tm.Teacher.FullName,
                    tm.Teacher.Email,
                    tm.Teacher.Image,
                    tm.Teacher.Phone,
                    tm.Teacher.Government,
                    tm.Teacher.StageKind,
                    tm.Teacher.Rating,
                    tm.Teacher.Status,
                    certifications = tm.Teacher.Certifications.Select(c => new
                    {
                        c.Id,
                        c.ImagePath,
                        c.Title,})
                }),
                lessons = m.Lessons.Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.VideoUrl,
                    l.VideoThumbnail,
                    l.Order,
                    l.CreatedAt
                }),
                exams=m.Exams.Select(e=>new {e.Id,e.Title,e.CreatedAt,e.Questions.Count}),
                //  exams = m.Exams.Select(e => new
                // {
                //     e.Id,
                //     e.Title,
                //     e.CreatedAt,
                //     questions = e.Questions.Select(q => new
                //     {
                //         q.Id,
                //         q.QuestionText,
                //         q.ImagePath,
                //         q.Order,
                //         choices = q.Choices.Select(c => new
                //         {
                //             c.Id,
                //             c.Text,
                //             c.IsCorrect
                //         }).ToList()
                //     }).ToList()
                materialSources = m.MaterialSources.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.SourceType,
                    s.Url,
                    s.PdfPath
                })
            });

            return Ok(result);
        }



    



        [Authorize(Roles="teacher")]
        [HttpDelete("Material/{materialId}")]
        public async Task<IActionResult> DeleteMaterial(int materialId){
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });
            
            var material = await _context.Materials
                .Include(m => m.TeacherMaterials)
                .FirstOrDefaultAsync(p => p.Id == materialId);
            
            if (material == null)
                return NotFound(new { message = "المادة غير موجودة." });
            
            if (!material.TeacherMaterials.Any(tm => tm.TeacherId == teacher.Id))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "لا يمكن حذف هذه المادة لأنها لا تنتمي إليك." });
            
            // Decrement the teacher's material counter
            teacher.MaterialCount = Math.Max(0, teacher.MaterialCount - 1);
            
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "تم حذف المادة بنجاح." });
        }























        // ────────────────────────────────────────────────────────────────
        // Private helpers
        // ────────────────────────────────────────────────────────────────
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
