using Microsoft.AspNetCore.Mvc;
using TaskApp.Models;
using TaskApp.Services;
using TaskApp.Validators;

namespace TaskApp.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IUserService _userService;

    public ProjectsController(IProjectService projectService, IUserService userService)
    {
        _projectService = projectService;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        return View(projects);
    }

    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();
        return View(project);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Users = await _userService.GetAllAsync();
        return View(new Project());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        var errors = ProjectValidator.Validate(project).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage!);

            ViewBag.Users = await _userService.GetAllAsync();
            return View(project);
        }

        await _projectService.CreateAsync(project);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();
        ViewBag.Users = await _userService.GetAllAsync();
        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Project project)
    {
        if (id != project.Id) return BadRequest();

        var errors = ProjectValidator.Validate(project).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage!);

            ViewBag.Users = await _userService.GetAllAsync();
            return View(project);
        }

        var updated = await _projectService.UpdateAsync(project);
        if (updated is null) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null) return NotFound();
        return View(project);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _projectService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
