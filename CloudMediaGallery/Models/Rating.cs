using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    /// <summary>
    /// Оцінка зображення користувачем (1-5 зірок)
    /// </summary>
    public class Rating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Score { get; set; }

        [Required]
        public int MediaFileId { get; set; }
        public MediaFile? MediaFile { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
