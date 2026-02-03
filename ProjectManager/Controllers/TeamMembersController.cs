using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManager.Models;
using ProjectManager.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    [Authorize]
    public class TeamMembersController : Controller
    {
        private readonly CosmosDbService<TeamMember> _memberService;
        private readonly CosmosDbService<Project> _projectService;
        private const int PageSize = 20;

        public TeamMembersController(CosmosDbService<TeamMember> memberService, CosmosDbService<Project> projectService)
        {
            _memberService = memberService;
            _projectService = projectService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? continuationToken = null)
        {
            string query = "SELECT * FROM c WHERE c.DocType = 'TeamMember' ORDER BY c.Name";
            var (items, token) = await _memberService.GetItemsPagedAsync(query, PageSize, continuationToken);
            ViewBag.ContinuationToken = token;
            return View(items);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new TeamMember());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeamMember model)
        {
            if (!ModelState.IsValid) return View(model);
            await _memberService.AddItemAsync(model, model.Id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var member = await _memberService.GetItemAsync(id, id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TeamMember model)
        {
            if (!ModelState.IsValid) return View(model);
            await _memberService.UpdateItemAsync(id, model, model.Id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var member = await _memberService.GetItemAsync(id, id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Remove references from projects
            string q = $"SELECT * FROM c WHERE c.DocType = 'Project' AND ARRAY_CONTAINS(c.TeamMemberIds, '{id}')";
            var projects = await _projectService.GetItemsAsync(q);
            foreach (var p in projects)
            {
                if (p.TeamMemberIds != null && p.TeamMemberIds.Remove(id))
                {
                    await _projectService.UpdateItemAsync(p.Id, p, p.Id);
                }
            }

            await _memberService.DeleteItemAsync(id, id);
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            var member = await _memberService.GetItemAsync(id, id);
            if (member == null) return NotFound();
            return View(member);
        }
    }
}
