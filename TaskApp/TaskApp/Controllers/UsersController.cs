using Microsoft.AspNetCore.Mvc;
using TaskApp.Models;
using TaskApp.Services;
using TaskApp.Validators;

namespace TaskApp.Controllers;

public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllAsync();
        return View(users);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return View(user);
    }

    public IActionResult Create() => View(new User());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        var errors = UserValidator.Validate(user).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage!);
            return View(user);
        }

        await _userService.CreateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User user)
    {
        if (id != user.Id) return BadRequest();

        var errors = UserValidator.Validate(user).ToList();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? string.Empty, error.ErrorMessage!);
            return View(user);
        }

        var updated = await _userService.UpdateAsync(user);
        if (updated is null) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _userService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
