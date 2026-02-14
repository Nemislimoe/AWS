using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    /// <summary>
    /// Лайк зображення користувачем
    /// </summary>
    public class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MediaFileId { get; set; }
        public MediaFile? MediaFile { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
