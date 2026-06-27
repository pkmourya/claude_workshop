using System.Net;
using System.Net.Http.Json;
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

public class TasksApiTests : IAsyncLifetime
{
    private readonly TaskAppFactory _factory = new();

    private int    _aliceId;
    private string _aliceEmail = "alice@tasks.test";
    private int    _projectId;
    private int    _taskId;

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
        db.Users.Add(alice);
        await db.SaveChangesAsync();
        _aliceId = alice.Id;

        var project = new Project { Name = "Alpha", OwnerId = _aliceId };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        _projectId = project.Id;

        var task = new TaskItem
        {
            Title     = "Existing task",
            Status    = TaskStatus.Todo,
            Priority  = Priority.Medium,
            OwnerId   = _aliceId,
            ProjectId = _projectId
        };
        db.TaskItems.Add(task);

        db.TaskItems.Add(new TaskItem
        {
            Title     = "Done task",
            Status    = TaskStatus.Done,
            Priority  = Priority.High,
            OwnerId   = _aliceId,
            ProjectId = _projectId
        });
        await db.SaveChangesAsync();
        _taskId = task.Id;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Alice() =>
        _factory.CreateJsonClient().AuthorizeAs(_aliceId, _aliceEmail);

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    // ── GET /api/projects/{id}/tasks ──────────────────────────────────────────

    [Fact]
    public async Task GetByProject_ValidProject_Returns200WithTasks()
    {
        var res = await Alice().GetAsync($"/api/projects/{_projectId}/tasks");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var tasks = JsonSerializer.Deserialize<List<TaskItem>>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;
        Assert.Equal(2, tasks.Count);
    }

    [Fact]
    public async Task GetByProject_StatusFilter_ReturnsFilteredTasks()
    {
        var res = await Alice().GetAsync($"/api/projects/{_projectId}/tasks?status=Done");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var tasks = JsonSerializer.Deserialize<List<TaskItem>>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;
        Assert.All(tasks, t => Assert.Equal(TaskStatus.Done, t.Status));
    }

    [Fact]
    public async Task GetByProject_PriorityFilter_ReturnsFilteredTasks()
    {
        var res = await Alice().GetAsync($"/api/projects/{_projectId}/tasks?priority=High");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var tasks = JsonSerializer.Deserialize<List<TaskItem>>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;
        Assert.All(tasks, t => Assert.Equal(Priority.High, t.Priority));
    }

    [Fact]
    public async Task GetByProject_NonExistentProject_Returns404()
    {
        var res = await Alice().GetAsync("/api/projects/99999/tasks");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetByProject_Unauthenticated_Returns401()
    {
        var res = await _factory.CreateJsonClient().GetAsync($"/api/projects/{_projectId}/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── POST /api/projects/{id}/tasks ─────────────────────────────────────────

    [Fact]
    public async Task CreateTask_ValidRequest_Returns201()
    {
        var res = await Alice().PostAsync(
            $"/api/projects/{_projectId}/tasks",
            Json(new { title = "New task", priority = "Medium" }));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_StatusDefaultsToTodo()
    {
        var res = await Alice().PostAsync(
            $"/api/projects/{_projectId}/tasks",
            Json(new { title = "Auto-todo task", priority = "Low" }));

        res.EnsureSuccessStatusCode();
        var task = JsonSerializer.Deserialize<TaskItem>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;
        Assert.Equal(TaskStatus.Todo, task.Status);
    }

    [Fact]
    public async Task CreateTask_EmptyTitle_Returns400()
    {
        var res = await Alice().PostAsync(
            $"/api/projects/{_projectId}/tasks",
            Json(new { title = "", priority = "Low" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_NonExistentProject_Returns404()
    {
        var res = await Alice().PostAsync(
            "/api/projects/99999/tasks",
            Json(new { title = "Task", priority = "Low" }));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_Unauthenticated_Returns401()
    {
        var res = await _factory.CreateJsonClient().PostAsync(
            $"/api/projects/{_projectId}/tasks",
            Json(new { title = "Task", priority = "Low" }));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── GET /api/tasks/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ValidTask_Returns200()
    {
        var res = await Alice().GetAsync($"/api/tasks/{_taskId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var res = await Alice().GetAsync("/api/tasks/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task GetById_Unauthenticated_Returns401()
    {
        var res = await _factory.CreateJsonClient().GetAsync($"/api/tasks/{_taskId}");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── PUT /api/tasks/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTask_ValidRequest_Returns200WithUpdatedData()
    {
        var res = await Alice().PutAsync(
            $"/api/tasks/{_taskId}",
            Json(new { title = "Updated", status = "InProgress", priority = "High" }));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var task = JsonSerializer.Deserialize<TaskItem>(
            await res.Content.ReadAsStringAsync(), JsonOpts)!;
        Assert.Equal("Updated", task.Title);
        Assert.Equal(TaskStatus.InProgress, task.Status);
    }

    [Fact]
    public async Task UpdateTask_EmptyTitle_Returns400()
    {
        var res = await Alice().PutAsync(
            $"/api/tasks/{_taskId}",
            Json(new { title = "", status = "Todo", priority = "Low" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_NonExistent_Returns404()
    {
        var res = await Alice().PutAsync(
            "/api/tasks/99999",
            Json(new { title = "T", status = "Todo", priority = "Low" }));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_Unauthenticated_Returns401()
    {
        var res = await _factory.CreateJsonClient().PutAsync(
            $"/api/tasks/{_taskId}",
            Json(new { title = "T", status = "Todo", priority = "Low" }));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ── DELETE /api/tasks/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTask_ExistingTask_Returns204()
    {
        // Create a disposable task first to avoid affecting other tests
        var createRes = await Alice().PostAsync(
            $"/api/projects/{_projectId}/tasks",
            Json(new { title = "To delete", priority = "Low" }));
        var created = JsonSerializer.Deserialize<TaskItem>(
            await createRes.Content.ReadAsStringAsync(), JsonOpts)!;

        var res = await Alice().DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_NonExistent_Returns404()
    {
        var res = await Alice().DeleteAsync("/api/tasks/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_Unauthenticated_Returns401()
    {
        var res = await _factory.CreateJsonClient().DeleteAsync($"/api/tasks/{_taskId}");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
