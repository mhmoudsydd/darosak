using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace darsakApi.Models;

public class Student
{
    [Key]

    public int Id { get; set; }

    [Required(ErrorMessage = "حقل البريد الإلكتروني مطلوب.")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل الاسم الكامل مطلوب.")]
    [RegularExpression(@"^[\p{IsArabic}\s]{3,}$", ErrorMessage = "يجب أن يتكون الاسم من 3 أحرف عربية على الأقل ويسمح فقط بالحروف العربية والمسافات.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل الصورة مطلوب.")]
    [Url(ErrorMessage = "رابط الصورة غير صحيح.")]
    public string? Image { get; set; }

    [Required(ErrorMessage = "حقل هاتف الطالب مطلوب.")]
    [RegularExpression(@"^201[0125]\d{8}$", ErrorMessage = "صيغة رقم هاتف الطالب غير صحيحة.")]
    public string StudentPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل هاتف الأب مطلوب.")]
    [RegularExpression(@"^201[0125]\d{8}$", ErrorMessage = "صيغة رقم هاتف الأب غير صحيحة.")]
    public string FatherPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل الجنس مطلوب.")]
    [RegularExpression(@"^(ذكر|أنثى)$", ErrorMessage = "يجب أن يكون الجنس 'ذكر' أو 'أنثى'.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل المحافظة مطلوب.")]
    public string Government { get; set; } = string.Empty;

    // Client sends StageName + Department — server looks them up and sets StudentStageId
    [NotMapped]
    [Required(ErrorMessage = "حقل اسم المرحلة الدراسية مطلوب.")]
    public string StageName { get; set; } = string.Empty;

    [Required(ErrorMessage = "حقل القسم مطلوب.")]
    public string Department { get; set; } = string.Empty;

    // Set by server after validating StageName + Department
    public int StudentStageId { get; set; }
    public StudentStage? StudentStage { get; set; }

    [Required(ErrorMessage = "حقل معرف حساب المزود مطلوب.")]
    public string ProviderAccountId { get; set; } = string.Empty;
}
