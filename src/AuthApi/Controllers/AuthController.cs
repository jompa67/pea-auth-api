using System.Net;
using AuthApi.Contracts;
using AuthApi.Contracts.Login;
using AuthApi.Contracts.Register;
using AuthApi.Services;
using AuthApi.Services.Interface;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ILogger<AuthController> logger,
    ILoginService loginService,
    IValidator<LoginRequest> loginValidator,
    IValidator<RegisterWithPasswordRequest> registerValidator,
    IValidator<RefreshTokenRequest> refreshTokenValidator,
    IVerificationTokenService verificationTokenService)
    : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("CreateAdminUser")]
    public async Task<IActionResult> CreateAdminUser([FromQuery] string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var userProfile = await loginService.GetUserProfile(userName, cancellationToken);
   
            if(userProfile == null)
            {
                return BadRequest(new { message = "User does not exists" });
            }
            
            if(userProfile.IsAdminRole)
            {
                return Ok(new { message = "User is already registered as an Admin" });
            }
            
            var result = await loginService.CreateAdminUser(userName, cancellationToken);
            return Ok("Admin user created successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during admin user creation");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpPost("register/password")]
    public async Task<IActionResult> RegisterWithPassword([FromBody] RegisterWithPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await registerValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.ToDictionary(
                error => error.PropertyName,
                error => error.ErrorMessage));
        }
        
        try
        {
            var result = await loginService.RegisterWithPassword(request, cancellationToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { message = result.Message });
            }

            return StatusCode(StatusCodes.Status201Created, new { message = result.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Registration failed: {Error}", ex.Message);
            return BadRequest(new { message = "Registration failed. Please try again." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during registration");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpPost("login/password")]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await loginValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.ToDictionary(
                error => error.PropertyName,
                error => error.ErrorMessage));
        }
        
        try
        {
            var response = await loginService.LoginWithPassword(request, cancellationToken);

            if (!response.IsSuccess)
            {
                return Unauthorized(new { message = response.ErrorMessage });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for user {Username}", request.Username);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An unexpected error occurred. Please try again later." });
        }
    }
    
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutUser(CancellationToken cancellationToken = default)
    {
        try
        {
            var jwtToken = GetJwtTokenFromUser(); 
            var username = GetUsernameFromClaims(); 

            var response = await loginService.LogoutUser(username, cancellationToken);

            if (!response)
            {
                logger.LogWarning("Logout failed for user {Username}", username);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Logout failed. Please try again." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during logout");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An unexpected error occurred. Please try again later." });
        }
    }
    
    [HttpPost("refreshtoken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await refreshTokenValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.ToDictionary(
                error => error.PropertyName,
                error => error.ErrorMessage));
        }
        
        try
        {
            var refreshToken = request.RefreshToken;
            var token = request.Token;
            
            var response = await loginService.GetRefreshToken(token, refreshToken, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Token refresh failed: {Message}", response.Message);
                return StatusCode((int)response.StatusCode, new { message = response.Message });
            }

            return Ok(response.Response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("Email verification failed: Empty token provided");
            return BadRequest(new { message = "Invalid verification token" });
        }
        
        try
        {
            var userProfile = await verificationTokenService.ValidateEmailVerificationTokenAsync(token, cancellationToken);
            
            if (userProfile == null)
            {
                logger.LogWarning("Email verification failed: Invalid or expired token");
                return BadRequest(new { message = "Invalid or expired verification token" });
            }
            
            logger.LogInformation("Email verified successfully for user {Username}", userProfile.Username);
            return Ok(new { 
                message = "Email verified successfully", 
                username = userProfile.UsernameOriginal 
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during email verification");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An unexpected error occurred. Please try again later." });
        }
    }
    
    [Authorize]
    [HttpGet("protected")]
    public IActionResult ProtectedEndpoint(CancellationToken cancellationToken = default)
    {
        var username = User.Identity?.Name;
        logger.LogInformation("User {Username} accessed a protected endpoint.", username);
        return Ok(new { message = "You have accessed a protected endpoint!" });
    }
    
    private string? GetJwtTokenFromUser()
    {
        return HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private string? GetUsernameFromClaims()
    {
        return User.Identity?.Name;
    }

    [HttpGet("healthcheck")]
    public async Task<IActionResult> HealthCheck()
    {
        return Ok(new
        {
            message = "AuthApi is up and running",
            time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}