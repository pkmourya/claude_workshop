using Microsoft.AspNetCore.Mvc;

namespace TaskApp.Controllers;

public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
