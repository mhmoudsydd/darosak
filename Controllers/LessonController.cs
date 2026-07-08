using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using darsakApi.Data;
using darsakApi.Models;
using darsakApi.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace darsakApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class LessonController(AppDbContext context, AuthService authService) : ControllerBase
    {

        private readonly AppDbContext _context = context;
        private readonly AuthService _authService = authService;



        [Authorize(Roles = "teacher")]

        [HttpPost("lesson")]

        public async Task<IActionResult> CreateLesson([FromForm] Lesson request)
        {

            if (!ModelState.IsValid)
                return _authService.ValidationError(ModelState);

            // 1. Identify the logged-in teacher
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });

            // 2. Validate that the Material exists and belongs to the teacher
            var material = await _context.Materials
                .Include(m => m.TeacherMaterials)
                .FirstOrDefaultAsync(m => m.Id == request.MaterialId);

            if (material == null)
                return NotFound(new { message = "المادة الدراسية غير موجودة." });

            if (!material.TeacherMaterials.Any(tm => tm.TeacherId == teacher.Id))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "لا يمكنك إضافة درس لهذه المادة لأنها لا تنتمي إليك." });

            // 3. Populate fields and save the lesson
            request.TeacherId = teacher.Id;
            request.CreatedAt = DateOnly.FromDateTime(DateTime.Now);

            var date = ImpFunction.ToArabicNumbers(request.CreatedAt.ToString("yyyy/MM/dd"));



            _context.Lessons.Add(request);
            await _context.SaveChangesAsync();

            // 4. Increment the teacher's lesson count
            teacher.LessonCount += 1;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "تم إنشاء الدرس بنجاح",
                lesson =
                new
                {
                    request.Id,
                    request.Title,
                    request.VideoUrl,
                    request.VideoThumbnail,
                    request.MaterialId,
                    date,
                    request.TeacherId

                }
            });




        }



        [Authorize(Roles = "teacher")]
        [HttpGet("materials/{materialId}/lessons")]
        public async Task<IActionResult> MaterialLesson(int materialId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });
            var material = await _context.Materials.Include(m => m.TeacherMaterials).FirstOrDefaultAsync(m => m.Id == materialId);

            if (!material!.TeacherMaterials.Any(tm => tm.TeacherId == teacher.Id))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "لا يمكنك جلب الحصص لان المادة ليست تخصصك." });

            }
            var lessons = await _context.Lessons.Where(l => l.MaterialId == materialId).ToListAsync();

            return Ok(new
            {
                message = "تم جلب الحصص بنجاح",
                lessonCount = lessons.Count,
                lessons = lessons.Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.VideoUrl,
                    l.VideoThumbnail,
                    l.MaterialId,
                    date = ImpFunction.ToArabicNumbers(l.CreatedAt.ToString("yyyy/MM/dd")),
                    l.TeacherId

                })

            });


        }


        [Authorize(Roles = "teacher")]
        [HttpDelete("lesson/{lessonId}")]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية المعلم." });

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == email);
            if (teacher == null)
                return NotFound(new { message = "لم يتم العثور على المعلم." });
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound(new { message = "لم يتم العثور على الدرس." });
            if (lesson.TeacherId != teacher.Id)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "لا يمكنك حذف هذا الدرس لأنه ليس من اختصاصك." });
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            teacher.LessonCount -= 1;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "تم حذف الدرس بنجاح",
                lessonId
            });
        }


    }
}