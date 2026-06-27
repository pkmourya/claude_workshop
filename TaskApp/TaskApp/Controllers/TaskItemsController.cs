using Microsoft.AspNetCore.Mvc;
using TaskApp.Models;
using TaskApp.Services;
using TaskApp.Validators;

namespace TaskApp.Controllers;

public class TaskItemsController : Controller
{
    private readonly ITaskService _taskService;
    private readonly IUserService _userService;
    private readonly IProjectService _projectService;

    public TaskItemsController(ITaskService taskService, IUserService userService, IProjectService projectService)
    {
        _taskService = taskService;
        _userService = userService;
        _projectService = projectService;
    }

    public async Task<IActionResult> Index()
    {
        var tasks = await _taskService.GetAllAsync();
        return View(tasks);
    }

    public async Task<IActionResult> Details(int id)
    {
        var taskItem = await _taskService.GetByIdAsync(id);
        if (taskItem is null) return NotFound();
        return View(taskItem);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateViewBagsAsync();
        return View(new TaskItem());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskItem taskItem)
    {
        var errors = TaskItemValidator.Validate(taskItem).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage!);

            await PopulateViewBagsAsync();
            return View(taskItem);
        }

        await _taskService.CreateAsync(taskItem);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var taskItem = await _taskService.GetByIdAsync(id);
        if (taskItem is null) return NotFound();
        await PopulateViewBagsAsync();
        return View(taskItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskItem taskItem)
    {
        if (id != taskItem.Id) return BadRequest();

        var errors = TaskItemValidator.Validate(taskItem).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage!);

            await PopulateViewBagsAsync();
            return View(taskItem);
        }

        var updated = await _taskService.UpdateAsync(taskItem);
        if (updated is null) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var taskItem = await _taskService.GetByIdAsync(id);
        if (taskItem is null) return NotFound();
        return View(taskItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _taskService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async System.Threading.Tasks.Task PopulateViewBagsAsync()
    {
        ViewBag.Users = await _userService.GetAllAsync();
        ViewBag.Projects = await _projectService.GetAllAsync();
    }
}
