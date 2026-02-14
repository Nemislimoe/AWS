using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using CloudMediaGallery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBlobService _blobService;

        public AdminController(
            ApplicationDbContext db, 
            UserManager<ApplicationUser> userManager,
            IBlobService blobService)
        {
            _db = db;
            _userManager = userManager;
            _blobService = blobService;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalMedia = await _db.MediaFiles.CountAsync(),
                BlockedMedia = await _db.MediaFiles.CountAsync(m => m.IsBlocked),
                BlockedUsers = await _db.Users.CountAsync(u => u.IsBlocked),
                TotalCollections = await _db.Collections.CountAsync(),
                TotalComments = await _db.Comments.CountAsync()
            };

            return View(stats);
        }

        // Модерація медіафайлів
        public async Task<IActionResult> ModerateMedia(int page = 1, bool? showOnlyBlocked = null)
        {
            var query = _db.MediaFiles
                .Include(m => m.UploadedBy)
                .Include(m => m.MediaTags)
                .ThenInclude(mt => mt.Tag)
                .Include(m => m.Comments)
                .AsQueryable();

            if (showOnlyBlocked == true)
            {
                query = query.Where(m => m.IsBlocked);
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var media = await query
                .OrderByDescending(m => m.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.ShowOnlyBlocked = showOnlyBlocked;

            return View(media);
        }

        // Блокування медіафайлу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockMedia(int id, string reason)
        {
            var media = await _db.MediaFiles.FindAsync(id);
            if (media == null)
                return NotFound();

            media.IsBlocked = true;
            media.BlockedAt = DateTime.UtcNow;
            media.BlockReason = reason;

            await _db.SaveChangesAsync();

            return RedirectToAction("ModerateMedia");
        }

        // Розблокування медіафайлу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockMedia(int id)
        {
            var media = await _db.MediaFiles.FindAsync(id);
            if (media == null)
                return NotFound();

            media.IsBlocked = false;
            media.BlockedAt = null;
            media.BlockReason = null;

            await _db.SaveChangesAsync();

            return RedirectToAction("ModerateMedia");
        }

        // Видалення медіафайлу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _db.MediaFiles
                .Include(m => m.MediaTags)
                .Include(m => m.Comments)
                .Include(m => m.Likes)
                .Include(m => m.Ratings)
                .Include(m => m.CollectionMedias)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null)
                return NotFound();

            // Видаляємо з Blob Storage
            await _blobService.DeleteAsync(media.BlobUrl);

            // Видаляємо з БД
            _db.MediaFiles.Remove(media);
            await _db.SaveChangesAsync();

            return RedirectToAction("ModerateMedia");
        }

        // Управління користувачами
        public async Task<IActionResult> ManageUsers(int page = 1, bool? showOnlyBlocked = null)
        {
            var query = _db.Users.AsQueryable();

            if (showOnlyBlocked == true)
            {
                query = query.Where(u => u.IsBlocked);
            }

            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.JoinedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.ShowOnlyBlocked = showOnlyBlocked;

            return View(users);
        }

        // Блокування користувача
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string id, string reason)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = true;
            user.BlockedAt = DateTime.UtcNow;
            user.BlockReason = reason;

            await _userManager.UpdateAsync(user);

            return RedirectToAction("ManageUsers");
        }

        // Розблокування користувача
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = false;
            user.BlockedAt = null;
            user.BlockReason = null;

            await _userManager.UpdateAsync(user);

            return RedirectToAction("ManageUsers");
        }

        // Модерація коментарів
        public async Task<IActionResult> ModerateComments(int page = 1)
        {
            var pageSize = 20;
            var totalCount = await _db.Comments.CountAsync();
            var comments = await _db.Comments
                .Include(c => c.Author)
                .Include(c => c.MediaFile)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(comments);
        }

        // Видалення коментаря
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _db.Comments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
                return NotFound();

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            return RedirectToAction("ModerateComments");
        }

        // Статистика
        public async Task<IActionResult> Statistics()
        {
            var stats = new
            {
                UserStats = new
                {
                    Total = await _db.Users.CountAsync(),
                    Active = await _db.Users.CountAsync(u => !u.IsBlocked),
                    Blocked = await _db.Users.CountAsync(u => u.IsBlocked),
                    NewThisMonth = await _db.Users.CountAsync(u => u.JoinedAt >= DateTime.UtcNow.AddMonths(-1))
                },
                MediaStats = new
                {
                    Total = await _db.MediaFiles.CountAsync(),
                    Images = await _db.MediaFiles.CountAsync(m => m.MediaType == "image"),
                    Videos = await _db.MediaFiles.CountAsync(m => m.MediaType == "video"),
                    Blocked = await _db.MediaFiles.CountAsync(m => m.IsBlocked),
                    UploadedToday = await _db.MediaFiles.CountAsync(m => m.UploadedAt >= DateTime.UtcNow.Date)
                },
                EngagementStats = new
                {
                    TotalLikes = await _db.Likes.CountAsync(),
                    TotalComments = await _db.Comments.CountAsync(),
                    TotalRatings = await _db.Ratings.CountAsync(),
                    AverageRating = await _db.Ratings.AnyAsync() ? await _db.Ratings.AverageAsync(r => r.Score) : 0
                },
                CollectionStats = new
                {
                    Total = await _db.Collections.CountAsync(),
                    Public = await _db.Collections.CountAsync(c => c.IsPublic),
                    Private = await _db.Collections.CountAsync(c => !c.IsPublic),
                    TotalSubscriptions = await _db.CollectionSubscriptions.CountAsync()
                }
            };

            return View(stats);
        }
    }
}
