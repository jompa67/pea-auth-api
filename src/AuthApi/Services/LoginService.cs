using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthApi.Contracts.Login;
using AuthApi.Contracts.Register;
using AuthApi.Models;
using AuthApi.Models.Enums;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Interface;

namespace AuthApi.Services;

public class RegisterResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public int StatusCode { get; set; }
}

public class LoginService(
    ILogger<LoginService> logger,
    IJwtTokenGenerator jwtTokenGenerator,
    IUserProfileRepository userProfileRepository,
    IUserLoginRepository userLoginRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IEmailService emailService,
    IVerificationTokenService verificationTokenService,
    IBCryptWrapper bcryptWrapper
) : ILoginService
{
    public async Task<RegisterResult> RegisterWithPassword(RegisterWithPasswordRequest dto, CancellationToken cancellationToken)
    {
        var existingUser = await userProfileRepository.GetByUsernameAsync(dto.Username.ToLowerInvariant(), cancellationToken);
        if (existingUser != null)
        {
            return new RegisterResult
            {
                IsSuccess = false,
                Message = "Username is already taken",
                StatusCode = StatusCodes.Status409Conflict
            };
        }

        var existingEmail = await userProfileRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (existingEmail != null)
        {
            return new RegisterResult
            {
                IsSuccess = false,
                Message = "Email is already in use",
                StatusCode = StatusCodes.Status409Conflict
            };
        }

        var userId = Guid.NewGuid();

        var userProfile = new UserProfile
        {
            UserId = userId,
            Username = dto.Username.ToLowerInvariant(),
            UsernameOriginal = dto.Username,
            Email = dto.Email.ToLowerInvariant(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            EmailVerified = false,
            IsTestAccount = dto.IsTestAccount,
        };

        var emailVerificationToken = await verificationTokenService.GenerateEmailVerificationTokenAsync(userProfile, cancellationToken);

        await userProfileRepository.CreateUserProfile(userProfile, cancellationToken);

        var hashedPassword = bcryptWrapper.HashPassword(dto.Password);
        var login = new UserLogin
        {
            UserId = userId,
            AuthProvider = AuthProvider.Password,
            AuthType = AuthType.Password,
            AuthValue = hashedPassword
        };

        await userLoginRepository.CreateUserLogin(login);

        await emailService.SendVerificationEmailAsync(dto.Email, dto.Username, emailVerificationToken);

        var registrationSuccessMessage = $"User {dto.Username} registered successfully.";
        logger.LogInformation(registrationSuccessMessage, dto.Username);

        return new RegisterResult
        {
            IsSuccess = true,
            Message = registrationSuccessMessage,
            StatusCode = StatusCodes.Status201Created
        };
    }

    public async Task<LoginResponse> LoginWithPassword(LoginRequest dto, CancellationToken cancellationToken)
    {
        var username = dto.Username.ToLowerInvariant();
        var userProfile = await userProfileRepository.GetByUsernameAsync(username, cancellationToken);
        if (userProfile == null)
        {
            logger.LogWarning("Login failed for {Username}: User not found.", username);
            return new LoginResponse { IsSuccess = false, ErrorMessage = "Invalid credentials." };
        }
        
        if (userProfile.EmailVerified == false)
        {
            logger.LogWarning("Login failed for {Username}: Email not verified.", username);
            return new LoginResponse { IsSuccess = false, ErrorMessage = "Email not verified." };
        }

        var login = await userLoginRepository.GetByUserIdAndProviderAsync(userProfile.UserId, AuthProvider.Password);
        var isValid = bcryptWrapper.Verify(dto.Password, login.AuthValue);
        if (!isValid)
        {
            logger.LogWarning("Login failed for {Username}: Invalid password.", username);
            return new LoginResponse { IsSuccess = false, ErrorMessage = "Invalid credentials." };
        }

        logger.LogInformation($"User {username} logged in successfully.", username);

        var tokenResult = GenerateJwtToken(userProfile);
        var refreshToken = GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshTokenData
        {
            ExpiryDate = DateTime.UtcNow.AddHours(1),
            RefreshToken = refreshToken,
            Token = tokenResult.Token,
            UserId = username,
            IsUsed = false,
            IsRevoked = false
        }, cancellationToken);

        return new LoginResponse
        {
            Token = tokenResult.Token,
            RefreshToken = refreshToken,
            Expiration = tokenResult.Expiration,
            IsSuccess = true
        };
    }

    public async Task<(LoginResponse Response, HttpStatusCode StatusCode, string Message)> GetRefreshToken(string token,
        string oldRefreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(oldRefreshToken))
        {
            return (null, HttpStatusCode.BadRequest, "Token and refresh token are required.");
        }

        try
        {
            var storedToken = await refreshTokenRepository.GetByTokenAsync(oldRefreshToken, cancellationToken);

            if (storedToken is null)
            {
                logger.LogWarning("Refresh token not found: {RefreshToken}", oldRefreshToken);
                return (null, HttpStatusCode.Forbidden, "Invalid refresh token.");
            }

            if (storedToken.IsUsed || storedToken.IsRevoked)
            {
                logger.LogWarning(
                    "Attempted to use invalid refresh token: {RefreshToken}, IsUsed: {IsUsed}, IsRevoked: {IsRevoked}",
                    oldRefreshToken, storedToken.IsUsed, storedToken.IsRevoked);
                return (null, HttpStatusCode.Forbidden, "Refresh token has been used or revoked.");
            }

            if (storedToken.Token != token)
            {
                logger.LogWarning("Token mismatch for refresh token: {RefreshToken}", oldRefreshToken);
                return (null, HttpStatusCode.Forbidden, "Invalid token combination.");
            }

            if (storedToken.ExpiryDate < DateTime.UtcNow)
            {
                logger.LogWarning("Expired refresh token: {RefreshToken}, Expired: {ExpiryDate}",
                    oldRefreshToken, storedToken.ExpiryDate);
                return (null, HttpStatusCode.Forbidden, "Refresh token has expired.");
            }

            var userProfile = await userProfileRepository.GetByUsernameAsync(storedToken.UserId, cancellationToken);

            if (userProfile is null)
            {
                logger.LogWarning("User not found for refresh token: {RefreshToken}, UserId: {UserId}",
                    oldRefreshToken, storedToken.UserId);
                return (null, HttpStatusCode.Forbidden, "Invalid refresh token.");
            }

            var tokenResult = GenerateJwtToken(userProfile);
            var refreshToken = GenerateRefreshToken();

            await refreshTokenRepository.AddAsync(new RefreshTokenData
            {
                ExpiryDate = DateTime.UtcNow.AddDays(1),
                RefreshToken = refreshToken,
                Token = tokenResult.Token,
                UserId = storedToken.UserId,
                AddedDate = DateTime.UtcNow,
                IsUsed = false,
                IsRevoked = false
            }, cancellationToken);

            storedToken.IsUsed = true;
            await refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

            logger.LogInformation("Token refreshed successfully for user: {UserId}", storedToken.UserId);

            var loginResponse = new LoginResponse
            {
                Token = tokenResult.Token,
                RefreshToken = refreshToken,
                Expiration = tokenResult.Expiration,
                IsSuccess = true
            };

            return (loginResponse, HttpStatusCode.OK, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing token");
            return (null, HttpStatusCode.InternalServerError, "An error occurred while refreshing the token.");
        }
    }


    public async Task<bool> LogoutUser(string jwtToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(jwtToken))
        {
            logger.LogWarning("Logout attempted with empty jwtToken");
            return false;
        }

        try
        {
            // var tokenAsync = await refreshTokenRepository.GetByJwtTokenAsync(jwtToken);

            // Revoke all active refresh tokens for the user
            await refreshTokenRepository.RevokeAllForJwtTokenAsync(jwtToken, cancellationToken);

            logger.LogInformation("User {Username} logged out successfully", jwtToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout for user {Username}", jwtToken);
            return false;
        }
    }

    public async Task<bool> CreateAdminUser(string username, CancellationToken cancellationToken)
    {
        var profile = await userProfileRepository.GetByUsernameAsync(username, cancellationToken);
        
        if(profile is null)
        {
            logger.LogWarning("Admin user {Username} not found", username);
            return false;
        }
        
        profile.IsAdminRole = true;
        await userProfileRepository.UpdateAsync(profile, cancellationToken);
        logger.LogInformation("Admin user {Username} created successfully", username);
        return true;
    }

    public async Task<bool> IsUserAdmin(string userName, CancellationToken cancellationToken)
    {
        var userProfile = await userProfileRepository.GetByUsernameAsync(userName, cancellationToken);
        
        return userProfile?.IsAdminRole ?? false;
    }
    
    public async Task<bool> ExistsUser(string userName, CancellationToken cancellationToken)
    {
        var userProfile = await userProfileRepository.GetByUsernameAsync(userName, cancellationToken);
        
        return userProfile != null;
    }
    
    public async Task<UserProfile?> GetUserProfile(string userName, CancellationToken cancellationToken)
    {
        return await userProfileRepository.GetByUsernameAsync(userName, cancellationToken);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private TokenResult GenerateJwtToken(UserProfile user)
    {
        IEnumerable<Claim> claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("username", user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        if (user.IsUserRole)
            claims = JwtTokenGenerator.AddUserRole(claims);

        if (user.IsAdminRole)
            claims = JwtTokenGenerator.AddAdminRole(claims);

        var tokenResult = jwtTokenGenerator.GenerateToken(claims);

        return tokenResult;
    }
}