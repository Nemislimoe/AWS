using CloudMediaGallery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudMediaGallery.Models;

namespace CloudMediaGallery.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SearchController(ISearchService searchService, UserManager<ApplicationUser> userManager)
        {
            _searchService = searchService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return View("SearchForm");
            }

            var userId = User.Identity?.IsAuthenticated == true 
                ? _userManager.GetUserId(User) 
                : null;

            var results = await _searchService.SearchAsync(q, userId, page, pageSize: 20);

            ViewBag.Query = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(results.TotalCount / 20.0);

            return View("Results", results);
        }

        [HttpPost]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index", new { q = query });
        }
    }
}
