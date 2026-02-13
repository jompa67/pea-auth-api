using AuthApi.Models;

namespace AuthApi.Services.Interface;

public interface IVerificationTokenService
{
    Task<string> GenerateEmailVerificationTokenAsync(UserProfile userProfile, CancellationToken cancellationToken);
    Task<UserProfile> ValidateEmailVerificationTokenAsync(string token, CancellationToken cancellationToken);
}