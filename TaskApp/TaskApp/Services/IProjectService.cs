using TaskApp.Models;

namespace TaskApp.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(int id);
    Task<Project> CreateAsync(Project project);
    Task<Project?> UpdateAsync(Project project);
    Task<bool> DeleteAsync(int id);
}
