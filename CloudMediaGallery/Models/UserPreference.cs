using System.ComponentModel.DataAnnotations;

namespace CloudMediaGallery.Models
{
    /// <summary>
    /// Вподобання користувача для персоналізації пошуку
    /// </summary>
    public class UserPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Улюблені теги користувача
        /// </summary>
        public ICollection<UserFavoriteTag> FavoriteTags { get; set; } = new List<UserFavoriteTag>();
    }

    /// <summary>
    /// Зв'язок між користувачем та улюбленим тегом
    /// </summary>
    public class UserFavoriteTag
    {
        public int UserPreferenceId { get; set; }
        public UserPreference? UserPreference { get; set; }

        public int TagId { get; set; }
        public Tag? Tag { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Історія пошуку користувача
    /// </summary>
    public class SearchHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(200)]
        public string SearchQuery { get; set; } = "";

        [Required]
        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Кількість результатів, які повернув пошук
        /// </summary>
        public int ResultsCount { get; set; }
    }
}
