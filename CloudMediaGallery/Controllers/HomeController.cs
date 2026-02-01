using Microsoft.AspNetCore.Mvc;

namespace CloudMediaGallery.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Media");
        }
    }
}
