using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManager.Models;
using ProjectManager.Services;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly CosmosDbService<TaskItem> _taskService;
        private readonly CosmosDbService<TeamMember> _memberService;
        private const int PageSize = 10;

        public TasksController(CosmosDbService<TaskItem> taskService, CosmosDbService<TeamMember> memberService)
        {
            _taskService = taskService;
            _memberService = memberService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? continuationToken = null)
        {
            string query = "SELECT * FROM c WHERE c.DocType = 'Task' ORDER BY c.CreatedDate DESC";
            var (items, token) = await _taskService.GetItemsPagedAsync(query, PageSize, continuationToken);
            ViewBag.ContinuationToken = token;
            return View(items);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create(string projectId)
        {
            var model = new TaskItem { ProjectId = projectId };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem model)
        {
            if (!ModelState.IsValid) return View(model);
            await _taskService.AddItemAsync(model, model.ProjectId);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(string id, string projectId)
        {
            var task = await _taskService.GetItemAsync(id, projectId);
            if (task == null) return NotFound();

            TeamMember? assigned = null;
            if (!string.IsNullOrEmpty(task.AssignedTo))
                assigned = await _memberService.GetItemAsync(task.AssignedTo, task.AssignedTo);

            ViewBag.AssignedMember = assigned;
            return View(task);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, string projectId)
        {
            var task = await _taskService.GetItemAsync(id, projectId);
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TaskItem model)
        {
            if (!ModelState.IsValid) return View(model);
            await _taskService.UpdateItemAsync(id, model, model.ProjectId);
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id, string projectId)
        {
            var task = await _taskService.GetItemAsync(id, projectId);
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string projectId)
        {
            await _taskService.DeleteItemAsync(id, projectId);
            return RedirectToAction("Index");
        }
    }
}
