using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AuthApi.Controllers;
using AuthApi.Contracts;
using AuthApi.Contracts.Login;
using AuthApi.Contracts.Register;
using AuthApi.Models;
using AuthApi.Services;
using AuthApi.Services.Interface;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using System.Security.Claims;
using FluentAssertions;

namespace AuthApi.IntegrationTests.Controllers;

public class AuthControllerTest
{
    private readonly AuthController _controller;
    private readonly ILogger<AuthController> _loggerMock;
    private readonly ILoginService _loginServiceMock;
    private readonly IValidator<LoginRequest> _loginValidatorMock;
    private readonly IValidator<RegisterWithPasswordRequest> _registerValidatorMock;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidatorMock;
    private readonly IVerificationTokenService _verificationTokenServiceMock;
    private readonly IBCryptWrapper _bcryptWrapperMock;

    public AuthControllerTest()
    {
        _loggerMock = Substitute.For<ILogger<AuthController>>();
        _loginServiceMock = Substitute.For<ILoginService>();
        _loginValidatorMock = Substitute.For<IValidator<LoginRequest>>();
        _registerValidatorMock = Substitute.For<IValidator<RegisterWithPasswordRequest>>();
        _refreshTokenValidatorMock = Substitute.For<IValidator<RefreshTokenRequest>>();
        _verificationTokenServiceMock = Substitute.For<IVerificationTokenService>();
        _bcryptWrapperMock = Substitute.For<IBCryptWrapper>();

        _controller = new AuthController(
            _loggerMock,
            _loginServiceMock,
            _loginValidatorMock,
            _registerValidatorMock,
            _refreshTokenValidatorMock,
            _verificationTokenServiceMock);
    }

    [Fact]
    public async Task CreateAdminUser_UserDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        string userName = "nonExistentUser";
        _loginServiceMock
            .GetUserProfile(userName, Arg.Any<CancellationToken>())
            .Returns((UserProfile)null);
    
        // Act
        var result = await _controller.CreateAdminUser(userName);
    
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Fact]
    public async Task CreateAdminUser_UserAlreadyAdmin_ReturnsOk()
    {
        // Arrange
        var userName = "adminUser";
        var userProfile = new UserProfile
        {
            Username = userName,
            IsAdminRole = true,
            UserId = default,
            Email = "",
            UsernameOriginal = "",
            FirstName = "",
            LastName = ""
        };
        _loginServiceMock
            .GetUserProfile(userName, Arg.Any<CancellationToken>())
            .Returns(userProfile);
    
        // Act
        var result = await _controller.CreateAdminUser(userName);
    
        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
    
    [Fact]
    public async Task RegisterWithPassword_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterWithPasswordRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        _registerValidatorMock
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("PropertyName", "ErrorMessage")]));

        // Act
        var result = await _controller.RegisterWithPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Fact]
    public async Task RegisterWithPassword_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new RegisterWithPasswordRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        _registerValidatorMock
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _loginServiceMock
            .RegisterWithPassword(request, Arg.Any<CancellationToken>())
            .Returns(new RegisterResult { IsSuccess = true, StatusCode = 201, Message = "" });

        // Act
        var result = await _controller.RegisterWithPassword(request);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
    }
    
    [Fact]
    public async Task LoginWithPassword_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "Billy",
            Password = "Olsen"
        };
        _loginValidatorMock
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("PropertyName", "ErrorMessage")]));
    
        // Act
        var result = await _controller.LoginWithPassword(request);
    
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Fact]
    public async Task LoginWithPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "Billy",
            Password = "Carsson"
        };
        _loginValidatorMock
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    
        _loginServiceMock
            .LoginWithPassword(request, Arg.Any<CancellationToken>())
            .Returns(new LoginResponse { IsSuccess = true, ErrorMessage = "" });

        // Act
        var result = await _controller.LoginWithPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
    
    private void SetupUserIdentity(string username = "testuser", string email = "test@example.com")
    {
        // Create claims identity
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim("username", username)
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        // Set up HttpContext with the claims principal
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        
        // Add authorization header for methods that extract tokens
        httpContext.Request.Headers["Authorization"] = "Bearer test-jwt-token";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task LogoutUser_Success_ReturnsNoContent()
    {
        // Arrange
        _loginServiceMock
            .LogoutUser(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
    
        // Set up user identity and authorization header
        SetupUserIdentity();
    
        // Act
        var result = await _controller.LogoutUser();
    
        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        // Verify the correct token was passed to the service
        await _loginServiceMock.Received(1).LogoutUser("testuser", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task LogoutUser_Failure_ReturnsBadRequest()
    {
        // Arrange
        _loginServiceMock
            .LogoutUser(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
            
        // Set up user identity and authorization header
        SetupUserIdentity();
    
        // Act
        var result = await _controller.LogoutUser();
    
        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    
        // Verify the correct token was passed to the service
        await _loginServiceMock.Received(1).LogoutUser("testuser", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task RefreshToken_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            Token = "QWQW",
            RefreshToken = "WERT"
        };
        _refreshTokenValidatorMock
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("PropertyName", "ErrorMessage")]));
    
        // Act
        var result = await _controller.RefreshToken(request);
    
        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    [Fact]
    public async Task RefreshToken_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            Token = "PPP",
            RefreshToken = "QQQ"
        };
        _refreshTokenValidatorMock
            .ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    
        _loginServiceMock
            .GetRefreshToken(request.Token, request.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((null, HttpStatusCode.OK, null));
    
        // Act
        var result = await _controller.RefreshToken(request);
    
        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
    
    [Fact]
    public async Task VerifyEmail_EmptyToken_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.VerifyEmail(string.Empty);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}