using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace darsakApi.Models
{
    public class MaterialSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        [Required]
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        [Required]
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Disapproved"

        public DateOnly SubscribedAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    }
}
