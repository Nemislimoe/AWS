using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    /// <summary>
    /// Колекція зображень користувача
    /// </summary>
    public class Collection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string OwnerId { get; set; } = "";
        public ApplicationUser? Owner { get; set; }

        /// <summary>
        /// Чи є колекція публічною (доступна іншим користувачам)
        /// </summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// Зображення в цій колекції
        /// </summary>
        public ICollection<CollectionMedia> CollectionMedias { get; set; } = new List<CollectionMedia>();

        /// <summary>
        /// Підписники цієї колекції
        /// </summary>
        public ICollection<CollectionSubscription> Subscriptions { get; set; } = new List<CollectionSubscription>();
    }

    /// <summary>
    /// Зв'язок між колекцією та медіафайлом (many-to-many)
    /// </summary>
    public class CollectionMedia
    {
        public int CollectionId { get; set; }
        public Collection? Collection { get; set; }

        public int MediaFileId { get; set; }
        public MediaFile? MediaFile { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
