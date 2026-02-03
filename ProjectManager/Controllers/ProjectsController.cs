using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManager.Models;
using ProjectManager.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {

        private readonly CosmosDbService<Project> _projectService;
        private readonly CosmosDbService<TeamMember> _memberService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private const int PageSize = 10;

        public ProjectsController(
            CosmosDbService<Project> projectService,
            CosmosDbService<TeamMember> memberService,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _projectService = projectService;
            _memberService = memberService;
            _userManager = userManager;
            _signInManager = signInManager;
        }


        [AllowAnonymous]
        public async Task<IActionResult> Index(string? continuationToken = null)
        {
            string query = "SELECT * FROM c WHERE c.DocType = 'Project' ORDER BY c.Name";
            var (items, token) = await _projectService.GetItemsPagedAsync(query, PageSize, continuationToken);
            ViewBag.ContinuationToken = token;
            return View(items);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            if (!ModelState.IsValid) return View(model);
            await _projectService.AddItemAsync(model, model.Id);
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            var project = await _projectService.GetItemAsync(id, id);
            if (project == null) return NotFound();

            List<TeamMember> members = new();
            if (project.TeamMemberIds != null && project.TeamMemberIds.Any())
            {
                foreach (var mid in project.TeamMemberIds)
                {
                    var m = await _memberService.GetItemAsync(mid, mid);
                    if (m != null) members.Add(m);
                }
            }

            ViewBag.TeamMembers = members;
            return View(project);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            var project = await _projectService.GetItemAsync(id, id);
            if (project == null) return NotFound();
            return View(project);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Project model)
        {
            if (!ModelState.IsValid) return View(model);
            await _projectService.UpdateItemAsync(id, model, model.Id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var project = await _projectService.GetItemAsync(id, id);
            if (project == null) return NotFound();
            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var project = await _projectService.GetItemAsync(id, id);
            if (project == null) return NotFound();

            // Optionally: delete related tasks (not implemented here)
            await _projectService.DeleteItemAsync(id, id);
            return RedirectToAction(nameof(Index));
        }

        // TeamMember creation attached to project
        [Authorize]
        public IActionResult CreateMember(string projectId)
        {
            ViewBag.ProjectId = projectId;
            return View(new TeamMember());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMember(string projectId, TeamMember model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = projectId;
                return View(model);
            }

            var identityUser = await _userManager.FindByEmailAsync(model.Email);
            if (identityUser == null)
            {
                identityUser = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var tempPassword = "TempP@ssw0rd!";
                var createResult = await _userManager.CreateAsync(identityUser, tempPassword);
                if (!createResult.Succeeded)
                {
                    foreach (var err in createResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    ViewBag.ProjectId = projectId;
                    return View(model);
                }
            }

            if (!string.IsNullOrEmpty(model.Role))
            {
                var roleName = model.Role;
                if (!await _userManager.IsInRoleAsync(identityUser, roleName))
                {
                    var addRoleResult = await _userManager.AddToRoleAsync(identityUser, roleName);
                    if (!addRoleResult.Succeeded)
                    {
                        foreach (var err in addRoleResult.Errors)
                            ModelState.AddModelError(string.Empty, err.Description);

                        ViewBag.ProjectId = projectId;
                        return View(model);
                    }
                }
            }

            model.IdentityUserId = identityUser.Id;
            await _memberService.AddItemAsync(model, model.Id);

            var project = await _projectService.GetItemAsync(projectId, projectId);
            if (project != null)
            {
                project.TeamMemberIds ??= new List<string>();
                project.TeamMemberIds.Add(model.Id);
                await _projectService.UpdateItemAsync(project.Id, project, project.Id);
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == identityUser.Id)
                {
                    await _signInManager.RefreshSignInAsync(identityUser);
                }
            }

            return RedirectToAction("Details", new { id = projectId });
        }

    }
}
