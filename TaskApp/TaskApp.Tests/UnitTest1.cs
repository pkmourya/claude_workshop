using TaskApp.Models;
using TaskApp.Validators;

namespace TaskApp.Tests;

public class TaskItemValidatorTests
{
    [Fact]
    public void Validate_ValidTaskItem_ReturnsNoErrors()
    {
        var taskItem = new TaskItem
        {
            Title = "Write tests",
            OwnerId = 1,
            ProjectId = 1,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        var errors = TaskItemValidator.Validate(taskItem).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        var taskItem = new TaskItem { Title = "", OwnerId = 1, ProjectId = 1 };

        var errors = TaskItemValidator.Validate(taskItem).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaskItem.Title)));
    }

    [Fact]
    public void Validate_PastDueDate_ReturnsError()
    {
        var taskItem = new TaskItem
        {
            Title = "Overdue",
            OwnerId = 1,
            ProjectId = 1,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        var errors = TaskItemValidator.Validate(taskItem).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaskItem.DueDate)));
    }

    [Fact]
    public void Validate_InvalidOwnerId_ReturnsError()
    {
        var taskItem = new TaskItem { Title = "Test", OwnerId = 0, ProjectId = 1 };

        var errors = TaskItemValidator.Validate(taskItem).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TaskItem.OwnerId)));
    }
}

public class UserValidatorTests
{
    [Fact]
    public void Validate_ValidUser_ReturnsNoErrors()
    {
        var user = new User { Name = "Alice", Email = "alice@example.com" };

        var errors = UserValidator.Validate(user).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError()
    {
        var user = new User { Name = "Bob", Email = "not-an-email" };

        var errors = UserValidator.Validate(user).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(User.Email)));
    }
}

public class ProjectValidatorTests
{
    [Fact]
    public void Validate_ValidProject_ReturnsNoErrors()
    {
        var project = new Project { Name = "Alpha", OwnerId = 1 };

        var errors = ProjectValidator.Validate(project).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var project = new Project { Name = "", OwnerId = 1 };

        var errors = ProjectValidator.Validate(project).ToList();

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Project.Name)));
    }
}
