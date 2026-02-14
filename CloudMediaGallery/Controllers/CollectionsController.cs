using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Controllers
{
    [Authorize]
    public class CollectionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CollectionsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Список колекцій користувача
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var collections = await _db.Collections
                .Include(c => c.CollectionMedias)
                .ThenInclude(cm => cm.MediaFile)
                .Where(c => c.OwnerId == userId)
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ToListAsync();

            return View(collections);
        }

        // Публічні колекції інших користувачів
        public async Task<IActionResult> Explore()
        {
            var userId = _userManager.GetUserId(User);
            var collections = await _db.Collections
                .Include(c => c.Owner)
                .Include(c => c.CollectionMedias)
                .ThenInclude(cm => cm.MediaFile)
                .Include(c => c.Subscriptions)
                .Where(c => c.IsPublic && c.OwnerId != userId)
                .OrderByDescending(c => c.Subscriptions.Count)
                .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .ToListAsync();

            return View(collections);
        }

        // Перегляд колекції
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections
                .Include(c => c.Owner)
                .Include(c => c.CollectionMedias)
                .ThenInclude(cm => cm.MediaFile)
                .ThenInclude(m => m.MediaTags)
                .ThenInclude(mt => mt.Tag)
                .Include(c => c.Subscriptions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collection == null)
                return NotFound();

            // Перевірка доступу
            if (!collection.IsPublic && collection.OwnerId != userId)
                return Forbid();

            ViewBag.IsOwner = collection.OwnerId == userId;
            ViewBag.IsSubscribed = collection.Subscriptions.Any(s => s.SubscriberId == userId);

            return View(collection);
        }

        // Створення колекції
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? description, bool isPublic = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("Name", "Назва обов'язкова");
                return View();
            }

            var userId = _userManager.GetUserId(User);

            var collection = new Collection
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                IsPublic = isPublic,
                OwnerId = userId ?? "",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _db.Collections.Add(collection);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Помилка при створенні колекції: {ex.Message}");
                return View();
            }
        }

        // Редагування колекції
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == userId);

            if (collection == null)
                return NotFound();

            return View(collection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string? description, bool isPublic = false)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == userId);

            if (collection == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("Name", "Назва обов'язкова");
                return View(collection);
            }

            try
            {
                collection.Name = name.Trim();
                collection.Description = description?.Trim();
                collection.IsPublic = isPublic;
                collection.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Помилка при збереженні: {ex.Message}");
                return View(collection);
            }
        }

        // Видалення колекції
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections
                .Include(c => c.CollectionMedias)
                .Include(c => c.Subscriptions)
                .FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == userId);

            if (collection == null)
                return NotFound();

            _db.Collections.Remove(collection);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // Додавання медіафайлу до колекції
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedia(int collectionId, int mediaId)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections.FirstOrDefaultAsync(c => c.Id == collectionId && c.OwnerId == userId);

            if (collection == null)
                return NotFound();

            // Перевіряємо, чи медіафайл вже в колекції
            var exists = await _db.CollectionMedias.AnyAsync(cm => cm.CollectionId == collectionId && cm.MediaFileId == mediaId);

            if (!exists)
            {
                var collectionMedia = new CollectionMedia
                {
                    CollectionId = collectionId,
                    MediaFileId = mediaId,
                    AddedAt = DateTime.UtcNow
                };

                _db.CollectionMedias.Add(collectionMedia);
                collection.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = collectionId });
        }

        // Видалення медіафайлу з колекції
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMedia(int collectionId, int mediaId)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections.FirstOrDefaultAsync(c => c.Id == collectionId && c.OwnerId == userId);

            if (collection == null)
                return NotFound();

            var collectionMedia = await _db.CollectionMedias
                .FirstOrDefaultAsync(cm => cm.CollectionId == collectionId && cm.MediaFileId == mediaId);

            if (collectionMedia != null)
            {
                _db.CollectionMedias.Remove(collectionMedia);
                collection.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = collectionId });
        }

        // Підписка на колекцію
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int id)
        {
            var userId = _userManager.GetUserId(User);
            var collection = await _db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.IsPublic);

            if (collection == null || collection.OwnerId == userId)
                return NotFound();

            var exists = await _db.CollectionSubscriptions.AnyAsync(s => s.CollectionId == id && s.SubscriberId == userId);

            if (!exists)
            {
                var subscription = new CollectionSubscription
                {
                    CollectionId = id,
                    SubscriberId = userId ?? "",
                    SubscribedAt = DateTime.UtcNow
                };

                _db.CollectionSubscriptions.Add(subscription);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }

        // Відписка від колекції
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unsubscribe(int id)
        {
            var userId = _userManager.GetUserId(User);
            var subscription = await _db.CollectionSubscriptions
                .FirstOrDefaultAsync(s => s.CollectionId == id && s.SubscriberId == userId);

            if (subscription != null)
            {
                _db.CollectionSubscriptions.Remove(subscription);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id });
        }
    }
}
