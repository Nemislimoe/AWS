using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Services
{
    /// <summary>
    /// Результат пошуку
    /// </summary>
    public class SearchResult
    {
        public List<MediaFile> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public string Query { get; set; } = "";
    }

    /// <summary>
    /// Сервіс пошуку зображень
    /// </summary>
    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(string query, string? userId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    }

    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SearchService> _logger;

        public SearchService(ApplicationDbContext db, ILogger<SearchService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<SearchResult> SearchAsync(string query, string? userId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var result = new SearchResult { Query = query };

            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return result;
                }

                // Розбиваємо запит на ключові слова
                var keywords = query.ToLower()
                    .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct()
                    .ToList();

                if (!keywords.Any())
                {
                    return result;
                }

                // Базовий запит
                var mediaQuery = _db.MediaFiles
                    .Include(m => m.MediaTags)
                    .ThenInclude(mt => mt.Tag)
                    .Include(m => m.UploadedBy)
                    .Include(m => m.Likes)
                    .Include(m => m.Ratings)
                    .Where(m => !m.IsBlocked && m.IsApproved)
                    .AsQueryable();

                // Пошук за тегами, назвою файлу або описом
                mediaQuery = mediaQuery.Where(m =>
                    m.MediaTags.Any(mt => keywords.Contains(mt.Tag!.Name.ToLower())) ||
                    keywords.Any(k => m.FileName.ToLower().Contains(k)) ||
                    (m.Metadata != null && keywords.Any(k => m.Metadata.ToLower().Contains(k)))
                );

                // Підрахунок загальної кількості
                result.TotalCount = await mediaQuery.CountAsync(cancellationToken);

                // Сортування за релевантністю (кількість збігів з тегами)
                result.Results = await mediaQuery
                    .OrderByDescending(m => m.MediaTags.Count(mt => keywords.Contains(mt.Tag!.Name.ToLower())))
                    .ThenByDescending(m => m.Likes.Count)
                    .ThenByDescending(m => m.AverageRating)
                    .ThenByDescending(m => m.UploadedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Зберігаємо історію пошуку, якщо користувач авторизований
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var searchHistory = new SearchHistory
                        {
                            UserId = userId,
                            SearchQuery = query,
                            SearchedAt = DateTime.UtcNow,
                            ResultsCount = result.TotalCount
                        };

                        _db.SearchHistories.Add(searchHistory);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не вдалося зберегти історію пошуку");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час пошуку: {Query}", query);
            }

            return result;
        }
    }
}
