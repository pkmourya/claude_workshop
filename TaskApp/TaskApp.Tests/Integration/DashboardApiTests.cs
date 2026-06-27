using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskApp.Data;
using TaskApp.Models;
using TaskApp.Tests.Helpers;

namespace TaskApp.Tests.Integration;

public class DashboardApiTests : IAsyncLifetime
{
    private readonly TaskAppFactory _factory = new();

    private int    _aliceId;
    private string _aliceEmail = "alice@dashboard.test";
    private int    _projectId;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── Setup ─────────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var alice = new User { Name = "Alice", Email = _aliceEmail };
        alice.PasswordHash = hasher.HashPassword(alice, "Password1!");

        var bob = new User { Name = "Bob", Email = "bob@dashboard.test" };
        bob.PasswordHash = hasher.HashPassword(bob, "Password1!");

        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();
        _aliceId = alice.Id;

        var project = new Project { Name = "Alice Project", OwnerId = alice.Id };
        db.Projects.Add(project);

        var bobProject = new Project { Name = "Bob Project", OwnerId = bob.Id };
        db.Projects.Add(bobProject);

        await db.SaveChangesAsync();
        _projectId = project.Id;

        db.TaskItems.AddRange(
            new TaskItem { Title = "T1", Status = TaskApp.Models.TaskStatus.Todo,       Priority = Priority.Low,    OwnerId = alice.Id, ProjectId = _projectId },
            new TaskItem { Title = "T2", Status = TaskApp.Models.TaskStatus.InProgress, Priority = Priority.Medium, OwnerId = alice.Id, ProjectId = _projectId },
            new TaskItem { Title = "T3", Status = TaskApp.Models.TaskStatus.Done,       Priority = Priority.High,   OwnerId = alice.Id, ProjectId = _projectId }
        );
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_Authenticated_Returns200()
    {
        var client = _factory.CreateJsonClient().AuthorizeAs(_aliceId, _aliceEmail);
        var res = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_Unauthenticated_Returns401()
    {
        var client = _factory.CreateJsonClient();
        var res = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_ReturnsAlicesProjectWithCorrectCounts()
    {
        var client = _factory.CreateJsonClient().AuthorizeAs(_aliceId, _aliceEmail);
        var res = await client.GetAsync("/api/dashboard");
        res.EnsureSuccessStatusCode();

        var items = JsonSerializer.Deserialize<List<ProjectSummaryDto>>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;

        var aliceProject = Assert.Single(items, p => p.ProjectName == "Alice Project");
        Assert.Equal(3, aliceProject.Total);
        Assert.Equal(1, aliceProject.Todo);
        Assert.Equal(1, aliceProject.InProgress);
        Assert.Equal(1, aliceProject.Done);
    }

    [Fact]
    public async Task GetDashboard_DoesNotIncludeOtherUsersProjects()
    {
        var client = _factory.CreateJsonClient().AuthorizeAs(_aliceId, _aliceEmail);
        var res = await client.GetAsync("/api/dashboard");

        var items = JsonSerializer.Deserialize<List<ProjectSummaryDto>>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;

        Assert.DoesNotContain(items, p => p.ProjectName == "Bob Project");
    }
}
