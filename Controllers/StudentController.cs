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
    public class StudentController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [Authorize]
        [HttpGet("students")]
        public async Task<IActionResult> Students()
        {
            var students = await _context.Students.Include(s => s.StudentStage).ToListAsync();

            return Ok(new
            {
                message = "تم جلب بيانات الطلاب بنجاح",
                total = students.Count,
                students
            });
        }

        [Authorize]
        [HttpGet("students/{id}")]
        public async Task<IActionResult> Student(int id)
        {
            var student = await _context.Students.Include(s => s.StudentStage).FirstOrDefaultAsync(u => u.Id == id);

            if (student == null)
            {
                return NotFound(new { message = "الطالب غير موجود" });
            }

            return Ok(new
            {
                message = "تم جلب بيانات الطالب بنجاح",
                student
            });
        }

        [Authorize]
        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);

            if (student == null)
            {
                return NotFound(new { message = "الطالب غير موجود" });
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف الطالب بنجاح" });
        }

        [Authorize(Roles = "student")]
        [HttpPost("materials/{materialId}/subscribe")]
        public async Task<IActionResult> SubscribeToMaterial(int materialId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية الطالب." });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null)
                return NotFound(new { message = "لم يتم العثور على الطالب." });

            var material = await _context.Materials.FindAsync(materialId);
            if (material == null)
                return NotFound(new { message = "المادة غير موجودة." });

            // Find the teacher who created this material.
            // A material is linked to a teacher via TeacherMaterials.
            var teacherId = await _context.TeacherMaterials
                .Where(tm => tm.MaterialId == materialId)
                .Select(tm => tm.TeacherId)
                .FirstOrDefaultAsync();

            if (teacherId == 0)
                return BadRequest(new { message = "لم يتم العثور على المعلم المسؤول عن هذه المادة." });

            // Check if there is already a subscription
            var existingSub = await _context.MaterialSubscriptions
                .FirstOrDefaultAsync(ms => ms.StudentId == student.Id && ms.MaterialId == materialId);

            if (existingSub != null)
            {
                if (existingSub.Status == "Approved")
                {
                    return BadRequest(new { message = "أنت مشترك بالفعل في هذه المادة." });
                }
                else if (existingSub.Status == "Pending")
                {
                    return BadRequest(new { message = "طلب الاشتراك الخاص بك قيد الانتظار بالفعل." });
                }
                else // Disapproved: let them resubmit the request
                {
                    existingSub.Status = "Pending";
                    existingSub.SubscribedAt = DateOnly.FromDateTime(DateTime.UtcNow);
                    existingSub.Price = material.Price;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "تم إعادة إرسال طلب الاشتراك بنجاح وهو قيد الانتظار.", subscription = existingSub });
                }
            }
            var teacher = await _context.Teachers.FindAsync(teacherId);
            var stage = await _context.StudentStages.FindAsync(material.StudentStageId);

            var subscription = new MaterialSubscription
            {
                StudentId = student.Id,
                MaterialId = material.Id,
                TeacherId = teacherId,
                Price = material.Price,
                Status = "Pending",
                SubscribedAt = DateOnly.FromDateTime(DateTime.Now),
            };
            var price = ImpFunction.ToArabicNumbers(subscription.Price.ToString());
            var date = ImpFunction.ToArabicNumbers(subscription.SubscribedAt.ToString("yyyy/MM/dd"));

            _context.MaterialSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();



            return Ok(new
            {
                message = "تم إرسال طلب الاشتراك بنجاح وهو قيد الانتظار.",
                studentId=student.Id,

                subscription = new
                {
                    subscription.Id,
                    price,
                    subscription.Status,
                    date,
                    material = new
                    {
                        material.Id,
                        material.Name,
                        material.Description,
                        stage = new
                    {
                        stage!.Id,
                        stage.Stage,
                        stage.Department
                    }

                    },
                    teacher = new
                    {
                        teacher!.Id,
                        teacher.FullName,
                        teacher.Email,
                        teacher.Image,
                        teacher.Government,
                    },
                   


                },



            });
        }

        [Authorize(Roles = "student")]
        [HttpGet("students/subscriptions")]
        public async Task<IActionResult> GetStudentSubscriptions()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (email == null)
                return Unauthorized(new { message = "لم يتم التعرف على هوية الطالب." });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null)
                return NotFound(new { message = "لم يتم العثور على الطالب." });

            var subscriptions = await _context.MaterialSubscriptions
                .Include(ms => ms.Material).ThenInclude(m => m.StudentStage)
                .Include(ms => ms.Teacher)
                .Where(ms => ms.StudentId == student.Id)
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
                        ms.Material.Description,
                        Stage = ms.Material.StudentStage,
                    },
                    teacher = new
                    {
                        ms.Teacher.Id,
                        ms.Teacher.FullName,
                        ms.Teacher.Email,
                        ms.Teacher.Image
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                message = "تم جلب اشتراكات الطالب بنجاح",
                total = subscriptions.Count,
                subscriptions
            });
        }
    }
}
