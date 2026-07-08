using darsakApi.Data;
using darsakApi.Models;
using darsakApi.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace darsakApi.Controllers
{
    [Route("api")]
    [ApiController]
    public class ExamController(AppDbContext context, AuthService authService, IWebHostEnvironment env) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly AuthService _authService = authService;
        private readonly IWebHostEnvironment _env = env;

        // ────────────────────────────────────────────────────────────────
        // POST api/materials/{materialId}/exams
        //
        // Content-Type: application/json
        //
        // {
        //   "title": "اختبار الفصل الأول",
        //   "questions": [
        //     {
        //       "questionText": "ما عاصمة مصر؟",
        //       "order": 1,
        //       "imagePath": "uploads/exam-questions/123.jpg", // Optional
        //       "choices": [
        //         { "text": "القاهرة",     "isCorrect": true  },
        //         { "text": "الإسكندرية", "isCorrect": false },
        //         { "text": "أسوان",       "isCorrect": false },
        //         { "text": "الجيزة",      "isCorrect": false }
        //       ]
        //     }
        //   ]
        // }
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpPost("materials/{materialId}/exams")]
        public async Task<IActionResult> CreateExam(int materialId, [FromBody] Exam request)
        {
            if (!ModelState.IsValid)
                return _authService.ValidationError(ModelState);

            if (request.Questions == null || request.Questions.Count == 0)
                return BadRequest(new { message = "يجب أن يحتوي الاختبار على سؤال واحد على الأقل." });

            // 1. Identify teacher from JWT
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });

            // 2. Verify the material exists and belongs to this teacher
            var material = await _context.Materials
                .Include(m => m.TeacherMaterials)
                .FirstOrDefaultAsync(m => m.Id == materialId);

            if (material == null)
                return NotFound(new { message = "المادة غير موجودة." });

            if (!material.TeacherMaterials.Any(tm => tm.TeacherId == teacher.Id))
                return Forbid();

            // 3. Validate each question: exactly 4 choices, exactly 1 correct
            int questionIndex = 1;
            foreach (var q in request.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText))
                    return BadRequest(new { message = $"نص السؤال رقم {questionIndex} مطلوب." });

                if (q.Choices == null || q.Choices.Count != 4)
                    return BadRequest(new { message = $"السؤال رقم {questionIndex} يجب أن يحتوي على 4 إجابات بالضبط." });

                if (q.Choices.Count(c => c.IsCorrect) != 1)
                    return BadRequest(new { message = $"السؤال رقم {questionIndex} يجب أن يحتوي على إجابة صحيحة واحدة فقط." });

                questionIndex++;
            }
            
       

            var exam = new Exam
            {
                Title = request.Title,
                MaterialId = materialId,       // always from URL, not from client
                TeacherId = teacher.Id,        // always from JWT, not from client
                CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                Questions = request.Questions.Select((q, i) => new ExamQuestion
                {
                    QuestionText = q.QuestionText,
                    Order = q.Order,
                    ImagePath = q.ImagePath,
                    Choices = q.Choices.Select(c => new ExamChoice
                    {
                        Text = c.Text,
                        IsCorrect = c.IsCorrect
                    }).ToList()
                }).ToList()
            };

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            // 5. Return the created exam
            return Ok( new
            {
                message = "تم إنشاء الاختبار بنجاح.",
                exam = MapExamResponse(exam)
            });

        }

        // ────────────────────────────────────────────────────────────────
        // GET api/materials/{materialId}/exams
        // List all exams for a material (public)
        // ────────────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("materials/{materialId}/exams")]
        public async Task<IActionResult> GetMaterialExams(int materialId)
        {
            var materialExists = await _context.Materials.AnyAsync(m => m.Id == materialId);
            if (!materialExists)
                return NotFound(new { message = "المادة غير موجودة." });

            var exams = await _context.Exams
                .Where(e => e.MaterialId == materialId)
                .Include(e => e.Teacher)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                message = "تم جلب الاختبارات بنجاح.",
                total = exams.Count,
                exams = exams.Select(e => MapExamResponse(e))
            });
        }

        // ────────────────────────────────────────────────────────────────
        // GET api/exams/{examId}
        // Get a single exam with full questions & choices (public)
        // ────────────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("exams/{examId}")]
        public async Task<IActionResult> GetExam(int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Teacher)
                .Include(e => e.Material)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                return NotFound(new { message = "الاختبار غير موجود." });

            return Ok(new
            {
                message = "تم جلب الاختبار بنجاح.",
                exam = MapExamResponse(exam)
            });
        }

        // ────────────────────────────────────────────────────────────────
        // DELETE api/exams/{examId}
        // Delete an exam — only the teacher who created it can delete it
        // ────────────────────────────────────────────────────────────────
        [Authorize(Roles = "teacher")]
        [HttpDelete("exams/{examId}")]
        public async Task<IActionResult> DeleteExam(int examId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });

            var exam = await _context.Exams
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                return NotFound(new { message = "الاختبار غير موجود." });

            if (exam.TeacherId != teacher.Id)
                return Forbid();

            // Delete question images from disk
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            foreach (var q in exam.Questions)
            {
                if (!string.IsNullOrEmpty(q.ImagePath))
                {
                    var fullPath = Path.Combine(webRoot, q.ImagePath);
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
            }

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف الاختبار بنجاح." });
        }

        // ────────────────────────────────────────────────────────────────
        // Private helpers
        // ────────────────────────────────────────────────────────────────
        private object MapExamResponse(Exam exam) => new
        {
            exam.Id,
            exam.Title,
            exam.CreatedAt,
            teacher = exam.Teacher == null ? null : new
            {
                exam.Teacher.Id,
                exam.Teacher.FullName,
                exam.Teacher.Email
            },
            material = exam.Material == null ? null : new
            {
                exam.Material.Id,
                exam.Material.Name
            },
            questions = exam.Questions
                .OrderBy(q => q.Order)
                .Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.Order,
                    q.ImagePath,
                    imageUrl = string.IsNullOrEmpty(q.ImagePath)
                        ? null
                        : $"{Request.Scheme}://{Request.Host}/{q.ImagePath}",
                    choices = q.Choices.Select(c => new
                    {
                        c.Id,
                        c.Text,
                        c.IsCorrect
                    })
                })
        };

    }
}
