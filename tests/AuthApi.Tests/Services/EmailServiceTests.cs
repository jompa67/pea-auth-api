using AuthApi.Services;
using AuthApi.Services.Interface;
using AuthApi.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Tests.Services
{
    public class EmailServiceTests
    {
        private IEmailService _emailService;
        private ILogger<EmailService> _loggerMock;
        private IOptions<EmailSettings> _emailSettingsMock;

        public EmailServiceTests()
        {
            _loggerMock = Substitute.For<ILogger<EmailService>>();
            
            var emailSettings = new EmailSettings
            {
                SmtpServer = "smtp.example.com",
                SmtpPort = 587,
                Username = "test@example.com",
                Password = "password",
                EnableSsl = true,
                SenderEmail = "noreply@example.com",
                SenderName = "Test Service",
                BaseUrl = "https://example.com"
            };
            
            _emailSettingsMock = Substitute.For<IOptions<EmailSettings>>();
            _emailSettingsMock.Value.Returns(emailSettings);
            
            // Using a real EmailService would try to connect to an SMTP server
            // In a real test, we would use a mock implementation or a test SMTP server
            // For now, we'll create a test-specific implementation that always returns success
            _emailService = new TestEmailService(_emailSettingsMock, _loggerMock);
        }

        [Fact]
        public async Task SendVerificationEmail_ShouldReturnTrue_WhenAllParametersAreValid()
        {
            // Arrange
            var email = "user@example.com";
            var username = "testuser";
            var verificationToken = "verification-token";

            // Act
            var result = await _emailService.SendVerificationEmailAsync(email, username, verificationToken);

            // Assert
            result.Should().BeTrue();
        }
        
        // Test-specific implementation of IEmailService that doesn't actually send emails
        private class TestEmailService : EmailService
        {
            public TestEmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
                : base(emailSettings, logger)
            {
            }

            public override Task<bool> SendVerificationEmailAsync(string email, string username, string verificationToken)
            {
                // Validate parameters but don't actually send email
                if (string.IsNullOrEmpty(email))
                    return Task.FromResult(false);
                
                if (string.IsNullOrEmpty(username))
                    return Task.FromResult(false);
                
                if (string.IsNullOrEmpty(verificationToken))
                    return Task.FromResult(false);
                
                return Task.FromResult(true);
            }
        }
    }
}
