using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace darsakApi.Models;


public class Teacher
{
    [Key]
    public int Id { get; set; }


    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
    [Required(ErrorMessage = "حقل البريد الإلكتروني مطلوب.")]
    public string Email { get; set; } = string.Empty;


    [Required(ErrorMessage = "حقل الاسم الكامل مطلوب.")]
    [RegularExpression(@"^[\p{IsArabic}\s]{3,25}$", ErrorMessage = "يجب أن يتكون الاسم من 3 أحرف عربية على الأقل ويسمح فقط بالحروف العربية والمسافات.")]
    public string FullName { get; set; } = string.Empty;

    /// Profile photo URL — can be set after sign-up
    [Url(ErrorMessage = "رابط الصورة غير صحيح.")]
    public string? Image { get; set; }


    [Required(ErrorMessage = "حقل هاتف المعلم مطلوب.")]
    [RegularExpression(@"^201[0125]\d{8}$", ErrorMessage = "صيغة رقم هاتف المعلم غير صحيحة.")]
    public string Phone { get; set; } = string.Empty;


    [Required(ErrorMessage = "حقل رقم البطاقة الشخصية مطلوبة.")]
    [RegularExpression(@"^\d{14}$", ErrorMessage = "رقم البطاقة الشخصية يتكون من 14 رقمًا.")]
    public string NationalId { get; set; } = string.Empty;


    [Required(ErrorMessage = "حقل الجنس مطلوب.")]
    [RegularExpression(@"^(ذكر|أنثى)$", ErrorMessage = "يجب أن يكون الجنس 'ذكر' أو 'أنثى'.")]
    public string Gender { get; set; } = string.Empty;


    [Required(ErrorMessage = "حقل المحافظة مطلوب.")]
    public string Government { get; set; } = string.Empty;


    [Required(ErrorMessage = "حقل نوع المرحلة الدراسية مطلوب.")]
    public string StageKind { get; set; } = string.Empty;




    [Required(ErrorMessage = "حقل معرف حساب المزود مطلوب.")]
    public string ProviderAccountId { get; set; } = string.Empty;




    public int StudentSubCount { get; set; } = 0;
    public int MaterialCount { get; set; } = 0;
    public int LessonCount { get; set; } = 0;

    public int RatingCount { get; set; } = 0;

    [NotMapped]
    public double Rating => RatingCount > 0 ? RatingCount : 0.0;

    public string Status { get; set; } = "قيد الانتظار";

    // Navigation: materials this teacher teaches (many-to-many via TeacherMaterial)
    public ICollection<TeacherMaterial> TeacherMaterials { get; set; } = [];

    // Navigation: lessons this teacher created
    public ICollection<Lesson> Lessons { get; set; } = [];

    // Navigation: certification images uploaded during sign-up


    public ICollection<TeacherCertification> Certifications { get; set; } = [];

    // Navigation: exams this teacher created
    public ICollection<Exam> Exams { get; set; } = [];

    // Navigation: posts this teacher uploaded
    public ICollection<Post> Posts { get; set; } = [];
}
