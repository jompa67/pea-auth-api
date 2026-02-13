using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthApi.Settings;
using System.Net;
using System.Net.Mail;
using AuthApi.Services.Interface;

namespace AuthApi.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public virtual async Task<bool> SendVerificationEmailAsync(string email, string username, string verificationToken)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
            };

            var verificationUrl = $"{_emailSettings.BaseUrl}/auth/api/auth/verify?token={verificationToken}";
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = "Verify Your Email Address",
                Body = $@"
                    <html>
                    <body>
                        <h2>Welcome, {username}!</h2>
                        <p>Thank you for registering with our service.</p>
                        <p>Please verify your email address by clicking the link below:</p>
                        <p><a href='{verificationUrl}'>Verify Email</a></p>
                        <p>This link will expire in 24 hours.</p>
                        <p>If you did not create an account, you can safely ignore this email.</p>
                    </body>
                    </html>
                ",
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(email);
            
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation($"Verification email sent to {email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send verification email to {email}: {ex.Message}");
            return false;
        }
    }
}
