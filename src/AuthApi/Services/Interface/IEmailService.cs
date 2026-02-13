namespace AuthApi.Services.Interface;

public interface IEmailService
{
    Task<bool> SendVerificationEmailAsync(string email, string username, string verificationToken);
}