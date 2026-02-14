using CloudMediaGallery.Data;
using CloudMediaGallery.Models;
using CloudMediaGallery.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudMediaGallery.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IRecommendationService _recommendationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext db,
            IRecommendationService recommendationService,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _recommendationService = recommendationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                
                // Отримуємо персоналізовані рекомендації
                var recommendations = await _recommendationService.GetRecommendationsAsync(userId ?? "", count: 12);
                
                ViewBag.PageTitle = "Рекомендовані для вас";
                return View(recommendations);
            }
            else
            {
                // Для неавторизованих показуємо популярні зображення
                var popularMedia = await _db.MediaFiles
                    .Include(m => m.MediaTags)
                    .ThenInclude(mt => mt.Tag)
                    .Include(m => m.Likes)
                    .Include(m => m.Ratings)
                    .Where(m => !m.IsBlocked && m.IsApproved)
                    .OrderByDescending(m => m.Likes.Count)
                    .ThenByDescending(m => m.AverageRating)
                    .ThenByDescending(m => m.ViewCount)
                    .Take(12)
                    .ToListAsync();

                ViewBag.PageTitle = "Популярні зображення";
                return View(popularMedia);
            }
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
