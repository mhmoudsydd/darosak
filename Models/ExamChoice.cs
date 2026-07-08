using System.ComponentModel.DataAnnotations;

namespace darsakApi.Models;

public class ExamChoice
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل نص الإجابة مطلوب.")]
    public string Text { get; set; } = string.Empty;

    // Exactly one choice per question must be true
    public bool IsCorrect { get; set; } = false;

    // FK → ExamQuestion
    public int QuestionId { get; set; }
    public ExamQuestion? Question { get; set; }
}
