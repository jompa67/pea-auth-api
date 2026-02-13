using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Interface;

namespace AuthApi.Services;

public class VerificationTokenService : IVerificationTokenService
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly TimeSpan _tokenValidity = TimeSpan.FromHours(24);

    public VerificationTokenService(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
    {
        // Generate a random token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // Set the token and expiry on the user profile
        userProfile.EmailVerificationToken = token;
        userProfile.EmailVerificationTokenExpiry = DateTime.UtcNow.Add(_tokenValidity);
        
        return token;
    }

    public async Task<UserProfile> ValidateEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        // Need to search for the user with this token
        // This is a simplified approach - in production, you'd likely want to index tokens
        // or have a separate verification store
        var allUsers = await _userProfileRepository.GetAllAsync(cancellationToken); 
        
        var user = allUsers.FirstOrDefault(u => 
            u.EmailVerificationToken == token && 
            u.EmailVerificationTokenExpiry > DateTime.UtcNow);
    
        if (user == null)
            return null;
    
        // Mark the user as verified
        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
    
        // Save the changes to the repository
        var updated = await _userProfileRepository.UpdateAsync(user, cancellationToken);
        if (!updated)
        {
            throw new InvalidOperationException($"Failed to update verification status for user {user.Username}");
        }
        
        return user;
    }
}
