using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace darsakApi.Models;

public enum SourceType
{
    Url,
    Pdf
}

public class MaterialSource
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل اسم المصدر مطلوب.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether this source is a URL or an uploaded PDF file.
    /// </summary>
    public SourceType SourceType { get; set; } = SourceType.Url;

    /// <summary>
    /// External URL — required when SourceType is Url.
    /// </summary>
    [Url(ErrorMessage = "رابط المصدر غير صحيح.")]
    public string? Url { get; set; }

    /// <summary>
    /// Server-side path of the uploaded PDF — required when SourceType is Pdf.
    /// Example: "uploads/materials/filename.pdf"
    /// </summary>
    [MaxLength(500)]
    public string? PdfPath { get; set; }

    // Which material this source belongs to
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
}
