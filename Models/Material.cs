using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace darsakApi.Models;

public class Material
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "حقل اسم المادة مطلوب.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل وصف المادة مطلوب.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل سعر الاشتراك مطلوب.")]
    [Range(0, 10000, ErrorMessage = "يجب أن يكون السعر بين 0 و 1000.")]
    [Column(TypeName = "int")]
    public int Price { get; set; }
    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    // Client sends StageName + Department — server looks them up and sets StudentStageId
    [NotMapped]
    [Required(ErrorMessage = "حقل اسم المرحلة الدراسية مطلوب.")]
    public string StageName { get; set; } = string.Empty;

    [NotMapped]


    [Required(ErrorMessage = "حقل القسم مطلوب.")]
    public string Department { get; set; } = string.Empty;

    // Set by server after validating StageName + Department
    public int StudentStageId { get; set; }
    public StudentStage? StudentStage { get; set; }

    // Many-to-Many: material can be taught by multiple teachers
    // Handled via TeacherMaterial junction table
    public ICollection<TeacherMaterial> TeacherMaterials { get; set; } = [];

    // One material has many lessons
    public ICollection<Lesson> Lessons { get; set; } = [];

    // One material has many sources (URLs or PDFs)
    public ICollection<MaterialSource> MaterialSources { get; set; } = [];

    // One material has many exams
    public ICollection<Exam> Exams { get; set; } = [];
}
