using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;
using TaskApp.Services;

namespace TaskApp.Tests.Unit;

public class ProjectServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (AppDbContext db, ProjectService svc, User user) Seed()
    {
        var db    = CreateDb();
        var user  = new User { Name = "Alice", Email = "alice@test.com" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Password1!");
        db.Users.Add(user);
        db.SaveChanges();
        return (db, new ProjectService(db), user);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllProjects()
    {
        var (db, svc, user) = Seed();
        await svc.CreateAsync(new Project { Name = "Alpha", OwnerId = user.Id });
        await svc.CreateAsync(new Project { Name = "Beta",  OwnerId = user.Id });

        var all = (await svc.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
        db.Dispose();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingProject_ReturnsIt()
    {
        var (db, svc, user) = Seed();
        var created = await svc.CreateAsync(new Project { Name = "Alpha", OwnerId = user.Id });

        var found = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal("Alpha", found!.Name);
        db.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var (db, svc, _) = Seed();
        Assert.Null(await svc.GetByIdAsync(999));
        db.Dispose();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Persists_WithGeneratedId()
    {
        var (db, svc, user) = Seed();
        var project = await svc.CreateAsync(new Project { Name = "Alpha", OwnerId = user.Id });

        Assert.True(project.Id > 0);
        Assert.Equal("Alpha", project.Name);
        db.Dispose();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingProject_ChangesName()
    {
        var (db, svc, user) = Seed();
        var project = await svc.CreateAsync(new Project { Name = "Alpha", OwnerId = user.Id });
        project.Name = "Renamed";

        var updated = await svc.UpdateAsync(project);

        Assert.NotNull(updated);
        Assert.Equal("Renamed", updated!.Name);
        db.Dispose();
    }

    [Fact]
    public async Task UpdateAsync_NonExistent_ReturnsNull()
    {
        var (db, svc, user) = Seed();
        var ghost = new Project { Id = 999, Name = "Ghost", OwnerId = user.Id };
        Assert.Null(await svc.UpdateAsync(ghost));
        db.Dispose();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingProject_ReturnsTrueAndRemoves()
    {
        var (db, svc, user) = Seed();
        var project = await svc.CreateAsync(new Project { Name = "Alpha", OwnerId = user.Id });

        var deleted = await svc.DeleteAsync(project.Id);
        Assert.True(deleted);
        Assert.Null(await svc.GetByIdAsync(project.Id));
        db.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsFalse()
    {
        var (db, svc, _) = Seed();
        Assert.False(await svc.DeleteAsync(999));
        db.Dispose();
    }
}
