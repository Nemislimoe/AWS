using Microsoft.AspNetCore.Mvc;
using ProjectManager.Models;
using ProjectManager.Services;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly CosmosDbService<Project> _projectService;
        private readonly CosmosDbService<TaskItem> _taskService;
        private const int RecentLimit = 5;

        public HomeController(CosmosDbService<Project> projectService, CosmosDbService<TaskItem> taskService)
        {
            _projectService = projectService;
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var projects = (await _projectService.GetItemsAsync("SELECT * FROM c WHERE c.DocType = 'Project'")).ToList();
            var tasks = (await _taskService.GetItemsAsync("SELECT * FROM c WHERE c.DocType = 'Task'")).ToList();

            var model = new DashboardViewModel
            {
                TotalProjects = projects.Count,
                TotalTasks = tasks.Count,
                RecentProjects = projects.OrderByDescending(p => p.StartDate).Take(RecentLimit).ToList(),
                RecentTasks = tasks.OrderByDescending(t => t.DueDate).Take(RecentLimit).ToList()
            };

            return View(model);
        }

        public IActionResult Privacy() => View();

        public IActionResult Error() => View();
    }
}
