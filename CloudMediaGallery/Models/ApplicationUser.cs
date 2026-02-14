using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    // Розширення IdentityUser для майбутніх полів
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Завантажені медіафайли
        /// </summary>
        public ICollection<MediaFile> UploadedMedia { get; set; } = new List<MediaFile>();

        /// <summary>
        /// Колекції користувача
        /// </summary>
        public ICollection<Collection> Collections { get; set; } = new List<Collection>();

        /// <summary>
        /// Коментарі користувача
        /// </summary>
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        /// <summary>
        /// Лайки користувача
        /// </summary>
        public ICollection<Like> Likes { get; set; } = new List<Like>();

        /// <summary>
        /// Оцінки користувача
        /// </summary>
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

        /// <summary>
        /// Підписки на колекції
        /// </summary>
        public ICollection<CollectionSubscription> CollectionSubscriptions { get; set; } = new List<CollectionSubscription>();

        /// <summary>
        /// Вподобання користувача
        /// </summary>
        public UserPreference? Preferences { get; set; }

        /// <summary>
        /// Історія пошуку
        /// </summary>
        public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();

        /// <summary>
        /// Чи заблокований користувач (для адмін-панелі)
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        public DateTime? BlockedAt { get; set; }

        [StringLength(500)]
        public string? BlockReason { get; set; }
    }
}
