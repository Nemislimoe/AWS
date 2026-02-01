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
        private readonly UserManager<ApplicationUser> _userManager;

        // Обмеження розміру файлу: 50 MB
        private const long MaxFileSize = 50L * 1024 * 1024;

        // Дозволені MIME типи
        private static readonly string[] AllowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        private static readonly string[] AllowedVideoTypes = new[] { "video/mp4", "video/webm", "video/ogg" };

        public MediaController(ApplicationDbContext db, IBlobService blobService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _blobService = blobService;
            _userManager = userManager;
        }

        // Галерея: фільтрація за типом та тегом
        public async Task<IActionResult> Index(string? type = null, int? tagId = null)
        {
            var userId = _userManager.GetUserId(User);
            var query = _db.MediaFiles
                .Include(m => m.MediaTags)
                    .ThenInclude(mt => mt.Tag)
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

        [HttpGet]
        public IActionResult Upload() => View();

        [HttpPost]
        [RequestSizeLimit(60_000_000)] // трохи більше за 50MB
        public async Task<IActionResult> Upload(IFormFile? file, string? tags)
        {
            if (file == null)
            {
                ModelState.AddModelError("", "Файл не вибрано.");
                return View();
            }

            if (file.Length == 0 || file.Length > MaxFileSize)
            {
                ModelState.AddModelError("", "Розмір файлу перевищує допустимий ліміт (50 MB).");
                return View();
            }

            var contentType = file.ContentType.ToLowerInvariant();
            string mediaType;
            if (AllowedImageTypes.Contains(contentType)) mediaType = "image";
            else if (AllowedVideoTypes.Contains(contentType)) mediaType = "video";
            else
            {
                ModelState.AddModelError("", "Непідтримуваний тип файлу. Дозволені: jpeg, png, gif, mp4, webm, ogg.");
                return View();
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
                UploadedById = userId ?? ""
            };

            // Обробка тегів: очікуємо рядок тегів через кому
            if (!string.IsNullOrWhiteSpace(tags))
            {
                var tagNames = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Select(t => t.Trim())
                                   .Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var tn in tagNames)
                {
                    var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tn.ToLower());
                    if (existing == null)
                    {
                        existing = new Tag { Name = tn };
                        _db.Tags.Add(existing);
                        await _db.SaveChangesAsync(); // зберігаємо, щоб отримати Id
                    }
                    media.MediaTags.Add(new MediaTag { TagId = existing.Id, Tag = existing });
                }
            }

            _db.MediaFiles.Add(media);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var media = await _db.MediaFiles.Include(m => m.MediaTags).FirstOrDefaultAsync(m => m.Id == id && m.UploadedById == userId);
            if (media == null) return NotFound();

            // Видаляємо з Blob
            await _blobService.DeleteAsync(media.BlobUrl);

            // Видаляємо з БД
            _db.MediaFiles.Remove(media);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
