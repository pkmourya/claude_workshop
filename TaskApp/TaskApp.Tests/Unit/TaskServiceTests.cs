using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;
using TaskApp.Services;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Tests.Unit;

public class TaskServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (AppDbContext db, TaskService svc, User user, Project project) Seed()
    {
        var db      = CreateDb();
        var hasher  = new PasswordHasher<User>();
        var user    = new User { Name = "Alice", Email = "alice@test.com" };
        user.PasswordHash = hasher.HashPassword(user, "Password1!");
        db.Users.Add(user);
        db.SaveChanges();

        var project = new Project { Name = "Alpha", OwnerId = user.Id };
        db.Projects.Add(project);
        db.SaveChanges();

        var svc = new TaskService(db);
        return (db, svc, user, project);
    }

    private static TaskItem MakeTask(int ownerId, int projectId,
        TaskStatus status = TaskStatus.Todo, Priority priority = Priority.Medium) =>
        new()
        {
            Title     = "Sample task",
            Status    = status,
            Priority  = priority,
            OwnerId   = ownerId,
            ProjectId = projectId
        };

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        var (db, svc, user, project) = Seed();
        await svc.CreateAsync(MakeTask(user.Id, project.Id));
        await svc.CreateAsync(MakeTask(user.Id, project.Id));

        var all = (await svc.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
        db.Dispose();
    }

    // ── GetByProjectAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByProjectAsync_NoFilter_ReturnsAll()
    {
        var (db, svc, user, project) = Seed();
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Todo));
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Done));

        var tasks = (await svc.GetByProjectAsync(project.Id, null, null)).ToList();
        Assert.Equal(2, tasks.Count);
        db.Dispose();
    }

    [Fact]
    public async Task GetByProjectAsync_StatusFilter_ReturnsMatching()
    {
        var (db, svc, user, project) = Seed();
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Todo));
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Done));

        var tasks = (await svc.GetByProjectAsync(project.Id, TaskStatus.Done, null)).ToList();
        Assert.Single(tasks);
        Assert.All(tasks, t => Assert.Equal(TaskStatus.Done, t.Status));
        db.Dispose();
    }

    [Fact]
    public async Task GetByProjectAsync_PriorityFilter_ReturnsMatching()
    {
        var (db, svc, user, project) = Seed();
        await svc.CreateAsync(MakeTask(user.Id, project.Id, priority: Priority.High));
        await svc.CreateAsync(MakeTask(user.Id, project.Id, priority: Priority.Low));

        var tasks = (await svc.GetByProjectAsync(project.Id, null, Priority.High)).ToList();
        Assert.Single(tasks);
        Assert.All(tasks, t => Assert.Equal(Priority.High, t.Priority));
        db.Dispose();
    }

    [Fact]
    public async Task GetByProjectAsync_BothFilters_ReturnsMatching()
    {
        var (db, svc, user, project) = Seed();
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Done, Priority.High));
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Done, Priority.Low));
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Todo, Priority.High));

        var tasks = (await svc.GetByProjectAsync(project.Id, TaskStatus.Done, Priority.High)).ToList();
        Assert.Single(tasks);
        Assert.Equal(TaskStatus.Done, tasks[0].Status);
        Assert.Equal(Priority.High,   tasks[0].Priority);
        db.Dispose();
    }

    [Fact]
    public async Task GetByProjectAsync_NoMatch_ReturnsEmpty()
    {
        var (db, svc, user, project) = Seed();
        await svc.CreateAsync(MakeTask(user.Id, project.Id, TaskStatus.Todo));

        var tasks = (await svc.GetByProjectAsync(project.Id, TaskStatus.Done, null)).ToList();
        Assert.Empty(tasks);
        db.Dispose();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingTask_ReturnsTask()
    {
        var (db, svc, user, project) = Seed();
        var created = await svc.CreateAsync(MakeTask(user.Id, project.Id));

        var found = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
        db.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var (db, svc, _, _) = Seed();
        Assert.Null(await svc.GetByIdAsync(999));
        db.Dispose();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Persists_WithGeneratedId()
    {
        var (db, svc, user, project) = Seed();
        var task = MakeTask(user.Id, project.Id);

        var created = await svc.CreateAsync(task);
        Assert.True(created.Id > 0);
        Assert.Equal("Sample task", created.Title);
        Assert.Equal(TaskStatus.Todo, created.Status);
        db.Dispose();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingTask_UpdatesFields()
    {
        var (db, svc, user, project) = Seed();
        var task = await svc.CreateAsync(MakeTask(user.Id, project.Id));

        task.Title  = "Updated title";
        task.Status = TaskStatus.InProgress;
        var updated = await svc.UpdateAsync(task);

        Assert.NotNull(updated);
        Assert.Equal("Updated title",      updated!.Title);
        Assert.Equal(TaskStatus.InProgress, updated.Status);
        db.Dispose();
    }

    [Fact]
    public async Task UpdateAsync_NonExistent_ReturnsNull()
    {
        var (db, svc, user, project) = Seed();
        var ghost = MakeTask(user.Id, project.Id);
        ghost.Id = 999;
        Assert.Null(await svc.UpdateAsync(ghost));
        db.Dispose();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingTask_ReturnsTrueAndRemoves()
    {
        var (db, svc, user, project) = Seed();
        var task = await svc.CreateAsync(MakeTask(user.Id, project.Id));

        var deleted = await svc.DeleteAsync(task.Id);
        Assert.True(deleted);
        Assert.Null(await svc.GetByIdAsync(task.Id));
        db.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsFalse()
    {
        var (db, svc, _, _) = Seed();
        Assert.False(await svc.DeleteAsync(999));
        db.Dispose();
    }
}
