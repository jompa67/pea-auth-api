using FluentValidation;
using AuthApi.Settings;

namespace AuthApi.Validators
{
    public class EmailSettingsValidator : AbstractValidator<EmailSettings>
    {
        public EmailSettingsValidator()
        {
            RuleFor(x => x.SmtpServer)
                .NotEmpty()
                .WithMessage("SMTP server cannot be empty");

            RuleFor(x => x.SmtpPort)
                .InclusiveBetween(1, 65535)
                .WithMessage("SMTP port must be between 1 and 65535");

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username cannot be empty");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password cannot be empty");

            RuleFor(x => x.SenderEmail)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("Sender email must be a valid email address");

            RuleFor(x => x.SenderName)
                .NotEmpty()
                .WithMessage("Sender name cannot be empty");

            RuleFor(x => x.BaseUrl)
                .NotEmpty()
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Base URL must be a valid URI");
        }
    }
}
