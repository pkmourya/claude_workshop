using System.ComponentModel.DataAnnotations;
using TaskApp.Models;

namespace TaskApp.Validators;

public static class UserValidator
{
    public static IEnumerable<ValidationResult> Validate(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
            yield return new ValidationResult("Name is required.", [nameof(User.Name)]);

        if (string.IsNullOrWhiteSpace(user.Email))
            yield return new ValidationResult("Email is required.", [nameof(User.Email)]);
        else if (!user.Email.Contains('@'))
            yield return new ValidationResult("Email is not valid.", [nameof(User.Email)]);
    }
}
