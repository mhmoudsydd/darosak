using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace darsakApi.Models;

public class ExamQuestion
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل نص السؤال مطلوب.")]
    public string QuestionText { get; set; } = string.Empty;

    // Optional image for the question (server-side path, e.g. "uploads/exam-questions/xyz.jpg")
    [MaxLength(500)]
    public string? ImagePath { get; set; }

    // Display order within the exam (1, 2, 3 ...)
    public int Order { get; set; } = 1;

    // FK → Exam
    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

    // Navigation: 4 answer choices for this question
    public ICollection<ExamChoice> Choices { get; set; } = [];
}
