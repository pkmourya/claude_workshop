using FluentValidation;
using TaskApp.Models;

namespace TaskApp.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority must be Low, Medium, or High.");

        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value >= DateTime.UtcNow.Date)
            .WithMessage("DueDate cannot be in the past.");
    }
}
