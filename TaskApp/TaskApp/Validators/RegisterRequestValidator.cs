using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;

namespace TaskApp.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(AppDbContext db)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MustAsync(async (email, ct) => !await db.Users.AnyAsync(u => u.Email == email, ct))
            .WithMessage("Email is already registered.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
    }
}
