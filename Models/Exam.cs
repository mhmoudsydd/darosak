using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace darsakApi.Models;

public class Exam
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل عنوان الاختبار مطلوب.")]
    public string Title { get; set; } = string.Empty;

    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    // FK → Material (which material this exam belongs to)
    public int MaterialId { get; set; }
    public Material? Material { get; set; }

    // FK → Teacher (who created this exam — must own the material)
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    // Navigation: questions in this exam
    public ICollection<ExamQuestion> Questions { get; set; } = [];
}
