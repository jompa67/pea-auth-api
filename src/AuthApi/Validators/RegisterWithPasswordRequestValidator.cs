using AuthApi.Contracts.Register;
using AuthApi.Settings;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AuthApi.Validators;

public class RegisterWithPasswordRequestValidator : AbstractValidator<RegisterWithPasswordRequest>
{
    public RegisterWithPasswordRequestValidator(IOptions<ValidationSettings> validationSettings)
    {
        var settings = validationSettings.Value;
        var passwordSettings = settings.Password;
        var userSettings = settings.User;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(userSettings.UsernameMinLength, userSettings.UsernameMaxLength)
            .WithMessage($"Username must be between {userSettings.UsernameMinLength} and {userSettings.UsernameMaxLength} characters");
     //       .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores and hyphens");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");

        // Password validation based on settings
        var passwordRule = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Length(passwordSettings.MinimumLength, passwordSettings.MaximumLength)
            .WithMessage($"Password must be between {passwordSettings.MinimumLength} and {passwordSettings.MaximumLength} characters");

        // Apply conditional rules based on settings
        if (passwordSettings.RequireUppercase)
        {
            passwordRule.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter");
        }

        if (passwordSettings.RequireLowercase)
        {
            passwordRule.Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter");
        }

        if (passwordSettings.RequireDigit)
        {
            passwordRule.Matches("[0-9]").WithMessage("Password must contain at least one digit");
        }

        if (passwordSettings.RequireNonAlphanumeric)
        {
            passwordRule.Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");
    }
}
