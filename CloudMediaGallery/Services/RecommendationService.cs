using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Services
{
    /// <summary>
    /// Сервіс для персоналізованих рекомендацій
    /// </summary>
    public interface IRecommendationService
    {
        Task<List<MediaFile>> GetRecommendationsAsync(string userId, int count = 10, CancellationToken cancellationToken = default);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(ApplicationDbContext db, ILogger<RecommendationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<MediaFile>> GetRecommendationsAsync(string userId, int count = 10, CancellationToken cancellationToken = default)
        {
            var recommendations = new List<MediaFile>();

            try
            {
                // 1. Отримуємо вподобання користувача
                var userPreference = await _db.UserPreferences
                    .Include(up => up.FavoriteTags)
                    .ThenInclude(ft => ft.Tag)
                    .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);

                // 2. Отримуємо історію пошуку користувача (останні 10 запитів)
                var searchHistory = await _db.SearchHistories
                    .Where(sh => sh.UserId == userId)
                    .OrderByDescending(sh => sh.SearchedAt)
                    .Take(10)
                    .Select(sh => sh.SearchQuery.ToLower())
                    .ToListAsync(cancellationToken);

                // 3. Отримуємо теги з вподобань і пошукових запитів
                var favoriteTagNames = userPreference?.FavoriteTags
                    .Select(ft => ft.Tag?.Name.ToLower())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList() ?? new List<string?>();

                var allRelevantTags = favoriteTagNames
                    .Concat(searchHistory)
                    .Distinct()
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                if (!allRelevantTags.Any())
                {
                    // Якщо немає історії, показуємо популярні зображення
                    recommendations = await _db.MediaFiles
                        .Include(m => m.MediaTags)
                        .ThenInclude(mt => mt.Tag)
                        .Include(m => m.Likes)
                        .Include(m => m.Ratings)
                        .Where(m => !m.IsBlocked && m.IsApproved)
                        .OrderByDescending(m => m.Likes.Count)
                        .ThenByDescending(m => m.AverageRating)
                        .ThenByDescending(m => m.ViewCount)
                        .Take(count)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    // 4. Знаходимо зображення з релевантними тегами
                    recommendations = await _db.MediaFiles
                        .Include(m => m.MediaTags)
                        .ThenInclude(mt => mt.Tag)
                        .Include(m => m.Likes)
                        .Include(m => m.Ratings)
                        .Where(m => !m.IsBlocked && 
                                   m.IsApproved && 
                                   m.UploadedById != userId && // Не показуємо власні зображення
                                   m.MediaTags.Any(mt => allRelevantTags.Contains(mt.Tag!.Name.ToLower())))
                        .OrderByDescending(m => m.MediaTags.Count(mt => allRelevantTags.Contains(mt.Tag!.Name.ToLower())))
                        .ThenByDescending(m => m.Likes.Count)
                        .ThenByDescending(m => m.AverageRating)
                        .Take(count)
                        .ToListAsync(cancellationToken);

                    // Якщо недостатньо рекомендацій, додаємо популярні
                    if (recommendations.Count < count)
                    {
                        var remainingCount = count - recommendations.Count;
                        var existingIds = recommendations.Select(r => r.Id).ToList();

                        var additional = await _db.MediaFiles
                            .Include(m => m.MediaTags)
                            .ThenInclude(mt => mt.Tag)
                            .Include(m => m.Likes)
                            .Include(m => m.Ratings)
                            .Where(m => !m.IsBlocked && 
                                       m.IsApproved && 
                                       !existingIds.Contains(m.Id))
                            .OrderByDescending(m => m.Likes.Count)
                            .ThenByDescending(m => m.AverageRating)
                            .Take(remainingCount)
                            .ToListAsync(cancellationToken);

                        recommendations.AddRange(additional);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при генерації рекомендацій");
                
                // У разі помилки повертаємо популярні зображення
                recommendations = await _db.MediaFiles
                    .Include(m => m.MediaTags)
                    .ThenInclude(mt => mt.Tag)
                    .Include(m => m.Likes)
                    .Include(m => m.Ratings)
                    .Where(m => !m.IsBlocked && m.IsApproved)
                    .OrderByDescending(m => m.UploadedAt)
                    .Take(count)
                    .ToListAsync(cancellationToken);
            }

            return recommendations;
        }
    }
}
