using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;
using TaskApp.Services;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Tests.Unit;

public class DashboardServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (AppDbContext db, DashboardService svc, User alice, User bob) Seed()
    {
        var db    = CreateDb();
        var h     = new PasswordHasher<User>();

        var alice = new User { Name = "Alice", Email = "alice@test.com" };
        alice.PasswordHash = h.HashPassword(alice, "pass");
        var bob   = new User { Name = "Bob",   Email = "bob@test.com"   };
        bob.PasswordHash   = h.HashPassword(bob,   "pass");

        db.Users.AddRange(alice, bob);
        db.SaveChanges();

        return (db, new DashboardService(db), alice, bob);
    }

    private static void AddTask(AppDbContext db, int ownerId, int projectId, TaskStatus status)
    {
        db.TaskItems.Add(new TaskItem
        {
            Title     = "t",
            Status    = status,
            Priority  = Priority.Low,
            OwnerId   = ownerId,
            ProjectId = projectId
        });
        db.SaveChanges();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_OnlyReturnsOwnProjects()
    {
        var (db, svc, alice, bob) = Seed();
        db.Projects.Add(new Project { Name = "Alice P", OwnerId = alice.Id });
        db.Projects.Add(new Project { Name = "Bob P",   OwnerId = bob.Id });
        db.SaveChanges();

        var result = (await svc.GetDashboardAsync(alice.Id)).ToList();

        Assert.Single(result);
        Assert.Equal("Alice P", result[0].ProjectName);
        db.Dispose();
    }

    [Fact]
    public async Task GetDashboard_CountsAllTasks()
    {
        var (db, svc, alice, _) = Seed();
        var p = new Project { Name = "P", OwnerId = alice.Id };
        db.Projects.Add(p);
        db.SaveChanges();

        AddTask(db, alice.Id, p.Id, TaskStatus.Todo);
        AddTask(db, alice.Id, p.Id, TaskStatus.InProgress);
        AddTask(db, alice.Id, p.Id, TaskStatus.Done);

        var result = (await svc.GetDashboardAsync(alice.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].Total);
        db.Dispose();
    }

    [Fact]
    public async Task GetDashboard_BreaksDownByStatus()
    {
        var (db, svc, alice, _) = Seed();
        var p = new Project { Name = "P", OwnerId = alice.Id };
        db.Projects.Add(p);
        db.SaveChanges();

        AddTask(db, alice.Id, p.Id, TaskStatus.Todo);
        AddTask(db, alice.Id, p.Id, TaskStatus.Todo);
        AddTask(db, alice.Id, p.Id, TaskStatus.InProgress);
        AddTask(db, alice.Id, p.Id, TaskStatus.Done);

        var r = (await svc.GetDashboardAsync(alice.Id)).Single();
        Assert.Equal(2, r.Todo);
        Assert.Equal(1, r.InProgress);
        Assert.Equal(1, r.Done);
        db.Dispose();
    }

    [Fact]
    public async Task GetDashboard_EmptyProject_ReturnsZeroCounts()
    {
        var (db, svc, alice, _) = Seed();
        db.Projects.Add(new Project { Name = "Empty", OwnerId = alice.Id });
        db.SaveChanges();

        var r = (await svc.GetDashboardAsync(alice.Id)).Single();
        Assert.Equal(0, r.Total);
        Assert.Equal(0, r.Todo);
        Assert.Equal(0, r.InProgress);
        Assert.Equal(0, r.Done);
        db.Dispose();
    }

    [Fact]
    public async Task GetDashboard_UserWithNoProjects_ReturnsEmpty()
    {
        var (db, svc, alice, _) = Seed();

        var result = await svc.GetDashboardAsync(alice.Id);
        Assert.Empty(result);
        db.Dispose();
    }

    [Fact]
    public async Task GetDashboard_DoesNotIncludeBobsProjectInAlicesDashboard()
    {
        var (db, svc, alice, bob) = Seed();
        db.Projects.AddRange(
            new Project { Name = "Alice P", OwnerId = alice.Id },
            new Project { Name = "Bob P",   OwnerId = bob.Id });
        db.SaveChanges();

        var aliceDash = (await svc.GetDashboardAsync(alice.Id)).ToList();
        Assert.DoesNotContain(aliceDash, p => p.ProjectName == "Bob P");
        db.Dispose();
    }
}
