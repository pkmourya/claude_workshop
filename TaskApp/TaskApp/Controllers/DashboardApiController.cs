using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Services;

namespace TaskApp.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardApiController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Sub claim missing from token."));

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var summary = await _dashboardService.GetDashboardAsync(CurrentUserId);
        return Ok(summary);
    }
}
