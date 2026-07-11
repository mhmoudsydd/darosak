using System.ComponentModel.DataAnnotations;

namespace darsakApi.Models;

public class Lesson
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل عنوان الدرس مطلوب.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل رابط الفيديو مطلوب.")]
    [Url(ErrorMessage = "رابط الفيديو غير صحيح.")]
    public string VideoUrl { get; set; } = string.Empty;

    [Url(ErrorMessage = "صورة الفيديو غير صحيحة.")]
    public string? VideoThumbnail { get; set; }

    // Display order of this lesson within the material (1, 2, 3...)
    public int Order { get; set; } = 1;

    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    // Which material this lesson belongs to
    public int MaterialId { get; set; }
    public Material? Material { get; set; }

    // Which teacher created/teaches this specific lesson
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
}
