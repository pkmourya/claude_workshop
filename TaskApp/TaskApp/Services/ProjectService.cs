using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;

namespace TaskApp.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Project>> GetAllAsync() =>
        await _db.Projects.Include(p => p.Owner).Include(p => p.Tasks).ToListAsync();

    public async Task<Project?> GetByIdAsync(int id) =>
        await _db.Projects.Include(p => p.Owner).Include(p => p.Tasks)
                          .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project> CreateAsync(Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<Project?> UpdateAsync(Project project)
    {
        var existing = await _db.Projects.FindAsync(project.Id);
        if (existing is null) return null;

        existing.Name = project.Name;
        existing.OwnerId = project.OwnerId;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }
}
