using FluentValidation;
using TaskApp.Models;
using TaskStatus = TaskApp.Models.TaskStatus;

namespace TaskApp.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be Todo, InProgress, or Done.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be Low, Medium, or High.");

        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value >= DateTime.UtcNow.Date)
            .WithMessage("DueDate cannot be in the past.");
    }
}
