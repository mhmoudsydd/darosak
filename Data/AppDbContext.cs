using Microsoft.EntityFrameworkCore;
using darsakApi.Models;

namespace darsakApi.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Student> Students { get; set; }
    public DbSet<StudentStage> StudentStages { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<TeacherMaterial> TeacherMaterials { get; set; }
    public DbSet<MaterialSource> MaterialSources { get; set; }
    public DbSet<TeacherCertification> TeacherCertifications { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<ExamChoice> ExamChoices { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<MaterialSubscription> MaterialSubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure composite primary key for the many-to-many junction table
        // Laravel equivalent: the pivot table with two foreign keys as the PK
        modelBuilder.Entity<TeacherMaterial>()
            .HasKey(tm => new { tm.TeacherId, tm.MaterialId });

        // Configure the two sides of the junction relationship
        modelBuilder.Entity<TeacherMaterial>()
            .HasOne(tm => tm.Teacher)
            .WithMany(t => t.TeacherMaterials)
            .HasForeignKey(tm => tm.TeacherId);

        modelBuilder.Entity<TeacherMaterial>()
            .HasOne(tm => tm.Material)
            .WithMany(m => m.TeacherMaterials)
            .HasForeignKey(tm => tm.MaterialId);

        modelBuilder.Entity<Post>()
            .Property(p => p.Tag)
            .HasConversion<string>();

        // MaterialSubscription relationships
        modelBuilder.Entity<MaterialSubscription>()
            .HasOne(ms => ms.Student)
            .WithMany()
            .HasForeignKey(ms => ms.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaterialSubscription>()
            .HasOne(ms => ms.Material)
            .WithMany()
            .HasForeignKey(ms => ms.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaterialSubscription>()
            .HasOne(ms => ms.Teacher)
            .WithMany()
            .HasForeignKey(ms => ms.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
