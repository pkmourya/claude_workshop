using TaskApp.Models;

namespace TaskApp.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<TaskItem> CreateAsync(TaskItem taskItem);
    Task<TaskItem?> UpdateAsync(TaskItem taskItem);
    Task<bool> DeleteAsync(int id);
}
