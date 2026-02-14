using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    public class MediaFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = "";

        [Required]
        public string ContentType { get; set; } = "";

        [Required]
        public string MediaType { get; set; } = ""; // "image" або "video"

        [Required]
        public DateTime UploadedAt { get; set; }

        [Required]
        public string BlobUrl { get; set; } = "";

        [Required]
        public string UploadedById { get; set; } = "";
        public ApplicationUser? UploadedBy { get; set; }

        /// <summary>
        /// Теги медіафайлу
        /// </summary>
        public ICollection<MediaTag> MediaTags { get; set; } = new List<MediaTag>();

        /// <summary>
        /// Колекції, до яких додано цей медіафайл
        /// </summary>
        public ICollection<CollectionMedia> CollectionMedias { get; set; } = new List<CollectionMedia>();

        /// <summary>
        /// Коментарі до цього медіафайлу
        /// </summary>
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        /// <summary>
        /// Лайки цього медіафайлу
        /// </summary>
        public ICollection<Like> Likes { get; set; } = new List<Like>();

        /// <summary>
        /// Оцінки цього медіафайлу
        /// </summary>
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

        /// <summary>
        /// Чи пройшов медіафайл модерацію контенту
        /// </summary>
        public bool IsModerated { get; set; } = false;

        /// <summary>
        /// Чи схвалений адміністратором (якщо потрібна пре-модерація)
        /// </summary>
        public bool IsApproved { get; set; } = true;

        /// <summary>
        /// Чи заблокований контент адміністратором
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        public DateTime? BlockedAt { get; set; }

        [StringLength(500)]
        public string? BlockReason { get; set; }

        /// <summary>
        /// Середній рейтинг (обчислюється)
        /// </summary>
        public double AverageRating { get; set; } = 0.0;

        /// <summary>
        /// Кількість переглядів
        /// </summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>
        /// Додаткові метадані (EXIF, геолокація тощо) - зберігаються як JSON
        /// </summary>
        public string? Metadata { get; set; }
    }
}
