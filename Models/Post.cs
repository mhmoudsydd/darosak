using System.ComponentModel.DataAnnotations;

namespace darsakApi.Models;

public enum PostTag
{
    إعلان,
    امتحان,
    مهم,
    واجب
}

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل عنوان المنشور مطلوب.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل محتوى المنشور مطلوب.")]
    public string Content { get; set; } = string.Empty;

    public PostTag Tag { get; set; } = PostTag.إعلان;

    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
}
