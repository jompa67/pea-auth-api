using AuthApi.Contracts;
using AuthApi.Settings;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AuthApi.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator(IOptions<ValidationSettings> validationSettings)
    {
        // We don't currently use specific validation settings for tokens,
        // but we inject the settings for consistency and future extensibility

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
