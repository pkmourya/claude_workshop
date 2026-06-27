using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;

namespace TaskApp.Services;

public class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync() =>
        await _db.TaskItems.Include(t => t.Owner).Include(t => t.Project).ToListAsync();

    public async Task<TaskItem?> GetByIdAsync(int id) =>
        await _db.TaskItems.Include(t => t.Owner).Include(t => t.Project)
                           .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TaskItem> CreateAsync(TaskItem taskItem)
    {
        _db.TaskItems.Add(taskItem);
        await _db.SaveChangesAsync();
        return taskItem;
    }

    public async Task<TaskItem?> UpdateAsync(TaskItem taskItem)
    {
        var existing = await _db.TaskItems.FindAsync(taskItem.Id);
        if (existing is null) return null;

        existing.Title = taskItem.Title;
        existing.Description = taskItem.Description;
        existing.Status = taskItem.Status;
        existing.Priority = taskItem.Priority;
        existing.DueDate = taskItem.DueDate;
        existing.OwnerId = taskItem.OwnerId;
        existing.ProjectId = taskItem.ProjectId;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var taskItem = await _db.TaskItems.FindAsync(id);
        if (taskItem is null) return false;

        _db.TaskItems.Remove(taskItem);
        await _db.SaveChangesAsync();
        return true;
    }
}
