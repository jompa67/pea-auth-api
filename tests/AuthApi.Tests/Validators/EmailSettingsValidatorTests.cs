using AuthApi.Validators;
using AuthApi.Settings;
using FluentValidation.TestHelper;
using Xunit;

namespace AuthApi.Tests.Validators
{
    public class EmailSettingsValidatorTests
    {
        private readonly EmailSettingsValidator _validator = new();

        [Fact]
        public void Should_Have_Error_When_SmtpServer_Is_Empty()
        {
            var model = new EmailSettings { SmtpServer = "", Username = "u", Password = "p", SenderEmail = "test@test.com", SenderName = "n", BaseUrl = "https://test.com" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.SmtpServer);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65536)]
        public void Should_Have_Error_When_SmtpPort_Is_Invalid(int port)
        {
            var model = new EmailSettings { SmtpServer = "s", SmtpPort = port, Username = "u", Password = "p", SenderEmail = "test@test.com", SenderName = "n", BaseUrl = "https://test.com" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.SmtpPort);
        }

        [Fact]
        public void Should_Have_Error_When_Username_Is_Empty()
        {
            var model = new EmailSettings { SmtpServer = "s", Username = "", Password = "p", SenderEmail = "test@test.com", SenderName = "n", BaseUrl = "https://test.com" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Username);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Empty()
        {
            var model = new EmailSettings { SmtpServer = "s", Username = "u", Password = "", SenderEmail = "test@test.com", SenderName = "n", BaseUrl = "https://test.com" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Should_Have_Error_When_SenderEmail_Is_Invalid()
        {
            var model = new EmailSettings { SmtpServer = "s", Username = "u", Password = "p", SenderEmail = "invalid", SenderName = "n", BaseUrl = "https://test.com" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.SenderEmail);
        }

        [Fact]
        public void Should_Have_Error_When_BaseUrl_Is_Invalid()
        {
            var model = new EmailSettings { SmtpServer = "s", Username = "u", Password = "p", SenderEmail = "test@test.com", SenderName = "n", BaseUrl = "invalid-url" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Settings_Are_Valid()
        {
            var model = new EmailSettings 
            { 
                SmtpServer = "smtp.example.com", 
                SmtpPort = 587, 
                Username = "user", 
                Password = "password", 
                SenderEmail = "test@example.com", 
                SenderName = "Test", 
                BaseUrl = "https://example.com" 
            };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
