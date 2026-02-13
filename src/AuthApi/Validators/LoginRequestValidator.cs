using AuthApi.Contracts.Login;
using AuthApi.Settings;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AuthApi.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator(IOptions<ValidationSettings> validationSettings)
    {
        var settings = validationSettings.Value;
        var userSettings = settings.User;
        var passwordSettings = settings.Password;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(userSettings.UsernameMinLength, userSettings.UsernameMaxLength)
            .WithMessage($"Username must be between {userSettings.UsernameMinLength} and {userSettings.UsernameMaxLength} characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(passwordSettings.MinimumLength)
            .WithMessage($"Password must be at least {passwordSettings.MinimumLength} characters");
    }
}
