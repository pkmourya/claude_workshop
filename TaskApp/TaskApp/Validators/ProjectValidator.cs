using System.ComponentModel.DataAnnotations;
using TaskApp.Models;

namespace TaskApp.Validators;

public static class ProjectValidator
{
    public static IEnumerable<ValidationResult> Validate(Project project)
    {
        if (string.IsNullOrWhiteSpace(project.Name))
            yield return new ValidationResult("Name is required.", [nameof(Project.Name)]);

        if (project.Name?.Length > 200)
            yield return new ValidationResult("Name cannot exceed 200 characters.", [nameof(Project.Name)]);

        if (project.OwnerId <= 0)
            yield return new ValidationResult("A valid OwnerId is required.", [nameof(Project.OwnerId)]);
    }
}
