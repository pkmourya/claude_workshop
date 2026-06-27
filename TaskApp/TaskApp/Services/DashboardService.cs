using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetDashboardAsync(int userId) =>
        await _db.Projects
            .Where(p => p.OwnerId == userId)
            .Select(p => new ProjectSummaryDto
            {
                ProjectId   = p.Id,
                ProjectName = p.Name,
                Total       = p.Tasks.Count,
                Todo        = p.Tasks.Count(t => t.Status == TaskStatus.Todo),
                InProgress  = p.Tasks.Count(t => t.Status == TaskStatus.InProgress),
                Done        = p.Tasks.Count(t => t.Status == TaskStatus.Done)
            })
            .ToListAsync();
}
