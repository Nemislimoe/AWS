using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using CloudMediaGallery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Controllers
{
    [Authorize]
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private readonly ICognitiveService _cognitiveService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MediaController> _logger;

        private const long MaxFileSize = 50L * 1024 * 1024; // 50 MB
        private static readonly string[] AllowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private static readonly string[] AllowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };

        public MediaController(
            ApplicationDbContext db, 
            IBlobService blobService,
            ICognitiveService cognitiveService,
            UserManager<ApplicationUser> userManager,
            ILogger<MediaController> logger)
        {
            _db = db;
            _blobService = blobService;
            _cognitiveService = cognitiveService;
            _userManager = userManager;
            _logger = logger;
        }

        // Галерея: фільтрація за типом та тегом
        public async Task<IActionResult> Index(string? type = null, int? tagId = null)
        {
            var userId = _userManager.GetUserId(User);
            var query = _db.MediaFiles
                .Include(m => m.MediaTags)
                .ThenInclude(mt => mt.Tag)
                .Include(m => m.Likes)
                .Include(m => m.Ratings)
                .Where(m => m.UploadedById == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(m => m.MediaType == type);
            }

            if (tagId.HasValue)
            {
                query = query.Where(m => m.MediaTags.Any(mt => mt.TagId == tagId.Value));
            }

            var tags = await _db.Tags.OrderBy(t => t.Name).ToListAsync();
            var items = await query.OrderByDescending(m => m.UploadedAt).ToListAsync();

            ViewBag.Tags = tags;
            ViewBag.SelectedType = type;
            ViewBag.SelectedTagId = tagId;

            return View("Gallery", items);
        }

        // Деталі медіафайлу
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var media = await _db.MediaFiles
                .Include(m => m.UploadedBy)
                .Include(m => m.MediaTags)
                .ThenInclude(mt => mt.Tag)
                .Include(m => m.Comments.Where(c => c.ParentCommentId == null))
                .ThenInclude(c => c.Author)
                .Include(m => m.Comments.Where(c => c.ParentCommentId == null))
                .ThenInclude(c => c.Replies)
                .ThenInclude(r => r.Author)
                .Include(m => m.Likes)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
                return NotFound();

            // Збільшуємо лічильник переглядів
            media.ViewCount++;
            await _db.SaveChangesAsync();

            // Перевіряємо, чи поставив користувач лайк
            ViewBag.UserLiked = media.Likes.Any(l => l.UserId == userId);

            // Отримуємо рейтинг користувача
            var userRating = await _db.Ratings.FirstOrDefaultAsync(r => r.MediaFileId == id && r.UserId == userId);
            ViewBag.UserRating = userRating?.Score ?? 0;

            // Отримуємо колекції користувача для додавання
            var userCollections = await _db.Collections
                .Where(c => c.OwnerId == userId)
                .ToListAsync();
            ViewBag.UserCollections = userCollections;

            return View(media);
        }

        [HttpGet]
        public IActionResult Upload() => View();

        [HttpPost]
        [RequestSizeLimit(60_000_000)] // трохи більше за 50MB
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file, string? tags)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Файл не вибрано.");
                return View();
            }

            if (file.Length > MaxFileSize)
            {
                ModelState.AddModelError("", "Розмір файлу перевищує допустимий ліміт (50 MB).");
                return View();
            }

            var contentType = file.ContentType.ToLowerInvariant();
            string mediaType;
            if (AllowedImageTypes.Contains(contentType)) 
                mediaType = "image";
            else if (AllowedVideoTypes.Contains(contentType)) 
                mediaType = "video";
            else
            {
                ModelState.AddModelError("", "Непідтримуваний тип файлу. Дозволені: jpeg, png, gif, webp, mp4, webm, ogg.");
                return View();
            }

            // Модерація контенту (тільки для зображень)
            ContentModerationResult? moderationResult = null;
            List<string> aiTags = new();
            string? aiDescription = null;

            if (mediaType == "image")
            {
                using (var stream = file.OpenReadStream())
                {
                    // Перевірка контенту
                    moderationResult = await _cognitiveService.ModerateImageAsync(stream);
                    
                    if (!moderationResult.IsApproved)
                    {
                        ModelState.AddModelError("", "Зображення не пройшло модерацію контенту. Виявлено неприпустимий вміст.");
                        _logger.LogWarning($"Image rejected by Content Moderator: Adult={moderationResult.IsAdultContent}, Racy={moderationResult.IsRacyContent}");
                        return View();
                    }

                    // Аналіз зображення для автоматичних тегів
                    (aiTags, aiDescription) = await _cognitiveService.AnalyzeImageAsync(stream);
                }
            }

            // Завантажуємо у Blob (або локально)
            string blobUrl;
            using (var stream = file.OpenReadStream())
            {
                blobUrl = await _blobService.UploadAsync(stream, file.FileName, contentType);
            }

            var userId = _userManager.GetUserId(User);
            var media = new MediaFile
            {
                FileName = file.FileName,
                ContentType = contentType,
                MediaType = mediaType,
                UploadedAt = DateTime.UtcNow,
                BlobUrl = blobUrl,
                UploadedById = userId ?? "",
                IsModerated = moderationResult != null,
                IsApproved = moderationResult?.IsApproved ?? true,
                Metadata = aiDescription
            };

            // Обробка тегів: об'єднуємо теги користувача та AI
            var allTags = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(tags))
            {
                allTags.AddRange(tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t)));
            }

            // Додаємо AI теги
            allTags.AddRange(aiTags);

            // Унікальні теги
            var uniqueTags = allTags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var tagName in uniqueTags)
            {
                var existingTag = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
                if (existingTag == null)
                {
                    existingTag = new Tag { Name = tagName };
                    _db.Tags.Add(existingTag);
                    await _db.SaveChangesAsync();
                }
                media.MediaTags.Add(new MediaTag { TagId = existingTag.Id, Tag = existingTag });
            }

            _db.MediaFiles.Add(media);
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = media.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var media = await _db.MediaFiles
                .Include(m => m.MediaTags)
                .Include(m => m.Comments)
                .Include(m => m.Likes)
                .Include(m => m.Ratings)
                .Include(m => m.CollectionMedias)
                .FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == userId);

            if (media == null) 
                return NotFound();

            // Видаляємо з Blob
            await _blobService.DeleteAsync(media.BlobUrl);

            // Видаляємо з БД (каскадне видалення спрацює для зв'язаних даних)
            _db.MediaFiles.Remove(media);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Лайк
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int id)
        {
            var userId = _userManager.GetUserId(User);
            var media = await _db.MediaFiles.FindAsync(id);

            if (media == null)
                return NotFound();

            var existingLike = await _db.Likes.FirstOrDefaultAsync(l => l.MediaFileId == id && l.UserId == userId);

            if (existingLike == null)
            {
                var like = new Like
                {
                    MediaFileId = id,
                    UserId = userId ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                _db.Likes.Add(like);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        // Видалення лайку
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(int id)
        {
            var userId = _userManager.GetUserId(User);
            var like = await _db.Likes.FirstOrDefaultAsync(l => l.MediaFileId == id && l.UserId == userId);

            if (like != null)
            {
                _db.Likes.Remove(like);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        // Рейтинг
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int id, int score)
        {
            if (score < 1 || score > 5)
                return BadRequest();

            var userId = _userManager.GetUserId(User);
            var media = await _db.MediaFiles
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
                return NotFound();

            var existingRating = await _db.Ratings.FirstOrDefaultAsync(r => r.MediaFileId == id && r.UserId == userId);

            if (existingRating == null)
            {
                var rating = new Rating
                {
                    MediaFileId = id,
                    UserId = userId ?? "",
                    Score = score,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Ratings.Add(rating);
            }
            else
            {
                existingRating.Score = score;
                existingRating.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Оновлюємо середній рейтинг
            media.AverageRating = media.Ratings.Any() ? media.Ratings.Average(r => r.Score) : 0;
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }

        // Коментар
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int id, string content, int? parentCommentId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest();

            var userId = _userManager.GetUserId(User);
            var media = await _db.MediaFiles.FindAsync(id);

            if (media == null)
                return NotFound();

            var comment = new Comment
            {
                MediaFileId = id,
                AuthorId = userId ?? "",
                Content = content,
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }

        // Видалення коментаря
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int mediaId)
        {
            var userId = _userManager.GetUserId(User);
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.AuthorId == userId);

            if (comment != null)
            {
                _db.Comments.Remove(comment);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = mediaId });
        }
    }
}
