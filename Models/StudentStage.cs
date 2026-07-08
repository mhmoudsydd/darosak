using System.ComponentModel.DataAnnotations;

namespace darsakApi.Models;

public class StudentStage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string Stage { get; set; }

    [Required]
    public required string Department { get; set; }

    // Reverse navigation — one StudentStage belongs to one Student
    // public Student? Student { get; set; }
}
