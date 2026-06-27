using TaskApp.Models;
using TaskApp.Validators;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Tests.Unit;

public class ValidatorTests
{

    // ── CreateTaskRequestValidator ───────────────────────────────────────────

    [Fact]
    public void CreateTask_ValidRequest_Passes()
    {
        var v = new CreateTaskRequestValidator();
        var req = new CreateTaskRequest { Title = "Fix bug", Priority = Priority.High };
        var result = v.Validate(req);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateTask_EmptyTitle_Fails()
    {
        var v = new CreateTaskRequestValidator();
        var result = v.Validate(new CreateTaskRequest { Title = "", Priority = Priority.Low });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateTask_TitleOver300_Fails()
    {
        var v = new CreateTaskRequestValidator();
        var result = v.Validate(new CreateTaskRequest { Title = new string('x', 301), Priority = Priority.Low });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateTask_PastDueDate_Fails()
    {
        var v = new CreateTaskRequestValidator();
        var result = v.Validate(new CreateTaskRequest
        {
            Title    = "Task",
            Priority = Priority.Medium,
            DueDate  = DateTime.UtcNow.Date.AddDays(-1)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DueDate");
    }

    [Fact]
    public void CreateTask_TodayDueDate_Passes()
    {
        var v = new CreateTaskRequestValidator();
        var result = v.Validate(new CreateTaskRequest
        {
            Title    = "Task",
            Priority = Priority.Medium,
            DueDate  = DateTime.UtcNow.Date
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateTask_NullDueDate_Passes()
    {
        var v = new CreateTaskRequestValidator();
        var result = v.Validate(new CreateTaskRequest { Title = "Task", Priority = Priority.Medium });
        Assert.True(result.IsValid);
    }

    // ── UpdateTaskRequestValidator ───────────────────────────────────────────

    [Fact]
    public void UpdateTask_ValidRequest_Passes()
    {
        var v = new UpdateTaskRequestValidator();
        var req = new UpdateTaskRequest
        {
            Title    = "Updated",
            Status   = TaskStatus.Done,
            Priority = Priority.High
        };
        Assert.True(v.Validate(req).IsValid);
    }

    [Fact]
    public void UpdateTask_EmptyTitle_Fails()
    {
        var v = new UpdateTaskRequestValidator();
        var result = v.Validate(new UpdateTaskRequest { Title = "", Status = TaskStatus.Todo, Priority = Priority.Low });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void UpdateTask_PastDueDate_Fails()
    {
        var v = new UpdateTaskRequestValidator();
        var result = v.Validate(new UpdateTaskRequest
        {
            Title    = "T",
            Status   = TaskStatus.InProgress,
            Priority = Priority.Low,
            DueDate  = DateTime.UtcNow.Date.AddDays(-1)
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DueDate");
    }

    // ── LoginRequestValidator ────────────────────────────────────────────────

    [Fact]
    public void Login_ValidRequest_Passes()
    {
        var v = new LoginRequestValidator();
        Assert.True(v.Validate(new LoginRequest { Email = "a@b.com", Password = "pass" }).IsValid);
    }

    [Fact]
    public void Login_EmptyEmail_Fails()
    {
        var v = new LoginRequestValidator();
        var result = v.Validate(new LoginRequest { Email = "", Password = "pass" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Login_InvalidEmailFormat_Fails()
    {
        var v = new LoginRequestValidator();
        var result = v.Validate(new LoginRequest { Email = "not-an-email", Password = "pass" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Login_EmptyPassword_Fails()
    {
        var v = new LoginRequestValidator();
        var result = v.Validate(new LoginRequest { Email = "a@b.com", Password = "" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    // ── RegisterRequestValidator ─────────────────────────────────────────────
    // Duplicate-email enforcement is handled by AuthService, not the validator.
    // See AuthServiceTests.Register_DuplicateEmail_ReturnsNull for that coverage.

    [Fact]
    public void Register_ValidRequest_Passes()
    {
        var v = new RegisterRequestValidator();
        var result = v.Validate(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_PasswordTooShort_Fails()
    {
        var v = new RegisterRequestValidator();
        var result = v.Validate(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "short" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void Register_EmptyName_Fails()
    {
        var v = new RegisterRequestValidator();
        var result = v.Validate(new RegisterRequest
            { Name = "", Email = "alice@test.com", Password = "Password1!" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Register_InvalidEmail_Fails()
    {
        var v = new RegisterRequestValidator();
        var result = v.Validate(new RegisterRequest
            { Name = "Alice", Email = "not-an-email", Password = "Password1!" });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}
