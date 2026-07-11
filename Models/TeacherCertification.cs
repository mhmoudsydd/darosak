using System.ComponentModel.DataAnnotations;

namespace darsakApi.Models;

public class TeacherCertification
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Optional label, e.g. "شهادة الليسانس" or "دبلوم تربوي"
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Relative path on the server, e.g. "uploads/certifications/abc.jpg"
    /// Served publicly via GET /{ImagePath}
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    // FK → Teacher
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
}
