using System.ComponentModel.DataAnnotations;
using TaskApp.Models;

namespace TaskApp.Validators;

public static class TaskItemValidator
{
    public static IEnumerable<ValidationResult> Validate(TaskItem taskItem)
    {
        if (string.IsNullOrWhiteSpace(taskItem.Title))
            yield return new ValidationResult("Title is required.", [nameof(TaskItem.Title)]);

        if (taskItem.Title?.Length > 300)
            yield return new ValidationResult("Title cannot exceed 300 characters.", [nameof(TaskItem.Title)]);

        if (taskItem.DueDate.HasValue && taskItem.DueDate.Value < DateTime.UtcNow.Date)
            yield return new ValidationResult("DueDate cannot be in the past.", [nameof(TaskItem.DueDate)]);

        if (taskItem.OwnerId <= 0)
            yield return new ValidationResult("A valid OwnerId is required.", [nameof(TaskItem.OwnerId)]);

        if (taskItem.ProjectId <= 0)
            yield return new ValidationResult("A valid ProjectId is required.", [nameof(TaskItem.ProjectId)]);
    }
}
