namespace darsakApi.Models;

// Junction table for Many-to-Many between Teacher and Material
// A teacher can teach many materials, and a material can be taught by many teachers
// Laravel equivalent: belongsToMany(Teacher::class) / belongsToMany(Material::class)
public class TeacherMaterial
{
    // Composite primary key: configured in AppDbContext
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

     
}
