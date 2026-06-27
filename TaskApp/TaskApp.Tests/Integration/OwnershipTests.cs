using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskApp.Data;
using TaskApp.Models;
using TaskApp.Tests.Helpers;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Tests.Integration;

/// <summary>
/// Negative ownership tests: User B cannot access User A's resources (expects 404).
/// </summary>
public class OwnershipTests : IAsyncLifetime
{
    private readonly TaskAppFactory _factory = new();

    private int    _aliceId;
    private int    _bobId;
    private string _aliceEmail = "alice@owner.test";
    private string _bobEmail   = "bob@owner.test";

    private int _aliceProjectId;
    private int _aliceTaskId;

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
        var bob = new User { Name = "Bob", Email = _bobEmail };
        bob.PasswordHash = hasher.HashPassword(bob, "Password1!");

        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();
        _aliceId = alice.Id;
        _bobId   = bob.Id;

        var aliceProject = new Project { Name = "Alice Project", OwnerId = _aliceId };
        db.Projects.Add(aliceProject);
        await db.SaveChangesAsync();
        _aliceProjectId = aliceProject.Id;

        var aliceTask = new TaskItem
        {
            Title     = "Alice Task",
            Status    = TaskStatus.Todo,
            Priority  = Priority.Medium,
            OwnerId   = _aliceId,
            ProjectId = _aliceProjectId
        };
        db.TaskItems.Add(aliceTask);
        await db.SaveChangesAsync();
        _aliceTaskId = aliceTask.Id;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Bob() =>
        _factory.CreateJsonClient().AuthorizeAs(_bobId, _bobEmail);

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    // ── Negative ownership tests (User B → User A's resources) ───────────────

    [Fact]
    public async Task Bob_CannotListTasksInAlicesProject_Returns404()
    {
        var res = await Bob().GetAsync($"/api/projects/{_aliceProjectId}/tasks");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Bob_CannotCreateTaskInAlicesProject_Returns404()
    {
        var res = await Bob().PostAsync(
            $"/api/projects/{_aliceProjectId}/tasks",
            Json(new { title = "Bob hijacking", priority = "Low" }));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Bob_CannotGetAlicesTask_Returns404()
    {
        var res = await Bob().GetAsync($"/api/tasks/{_aliceTaskId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Bob_CannotUpdateAlicesTask_Returns404()
    {
        var res = await Bob().PutAsync(
            $"/api/tasks/{_aliceTaskId}",
            Json(new { title = "Bob edit", status = "Done", priority = "Low" }));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Bob_CannotDeleteAlicesTask_Returns404()
    {
        var res = await Bob().DeleteAsync($"/api/tasks/{_aliceTaskId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Bob_DashboardDoesNotShowAlicesProject()
    {
        var res = await Bob().GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = JsonSerializer.Deserialize<List<ProjectSummaryDto>>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;

        Assert.DoesNotContain(items, p => p.ProjectName == "Alice Project");
    }

    [Fact]
    public async Task Alice_CanStillAccessHerOwnResourcesUnaffected()
    {
        var alice = _factory.CreateJsonClient().AuthorizeAs(_aliceId, _aliceEmail);

        var listRes = await alice.GetAsync($"/api/projects/{_aliceProjectId}/tasks");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        var taskRes = await alice.GetAsync($"/api/tasks/{_aliceTaskId}");
        Assert.Equal(HttpStatusCode.OK, taskRes.StatusCode);
    }
}
