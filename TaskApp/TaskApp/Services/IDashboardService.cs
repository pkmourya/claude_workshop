using TaskApp.Models;

namespace TaskApp.Services;

public interface IDashboardService
{
    Task<IEnumerable<ProjectSummaryDto>> GetDashboardAsync(int userId);
}
