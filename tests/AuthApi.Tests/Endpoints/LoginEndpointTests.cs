using AuthApi.Contracts.Login;
using AuthApi.Controllers;
using AuthApi.Services.Interface;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AuthApi.Tests.Endpoints
{
    public class LoginEndpointTests
    {
        private AuthController _controller;
        private ILogger<AuthController> _loggerMock;
        private ILoginService _loginServiceMock;
        private IValidator<LoginRequest> _loginValidatorMock;
        private IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest> _registerValidatorMock;
        private IValidator<AuthApi.Contracts.RefreshTokenRequest> _refreshTokenValidatorMock;
        private IVerificationTokenService _verificationTokenServiceMock;

        public LoginEndpointTests()
        {
            _loggerMock = Substitute.For<ILogger<AuthController>>();
            _loginServiceMock = Substitute.For<ILoginService>();
            _loginValidatorMock = Substitute.For<IValidator<LoginRequest>>();
            _registerValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest>>();
            _refreshTokenValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.RefreshTokenRequest>>();
            _verificationTokenServiceMock = Substitute.For<IVerificationTokenService>();

            _controller = new AuthController(
                _loggerMock,
                _loginServiceMock,
                _loginValidatorMock,
                _registerValidatorMock,
                _refreshTokenValidatorMock,
                _verificationTokenServiceMock
            );
        }

        [Fact]
        public async Task LoginWithPassword_WhenValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "", 
                Password = ""
            };
            
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Username", "Username is required"),
                new ValidationFailure("Password", "Password is required")
            };
            
            var validationResult = new ValidationResult(validationFailures);
            _loginValidatorMock.ValidateAsync(request).Returns(validationResult);

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errors = badRequestResult.Value.Should().BeAssignableTo<Dictionary<string, string>>().Subject;
            errors.Should().ContainKey("Username");
            errors.Should().ContainKey("Password");
        }

        [Fact]
        public async Task LoginWithPassword_WhenUserDoesNotExist_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest 
            { 
                Username = "nonexistentuser", 
                Password = "password123" 
            };
            
            var validationResult = new ValidationResult();
            _loginValidatorMock.ValidateAsync(request).Returns(validationResult);

            var loginResponse = new LoginResponse
            {
                IsSuccess = false,
                ErrorMessage = "Invalid credentials."
            };
            _loginServiceMock.LoginWithPassword(request, CancellationToken.None).Returns(loginResponse);

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            var responseBody = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task LoginWithPassword_WhenPasswordIsIncorrect_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest 
            { 
                Username = "existinguser", 
                Password = "wrongpassword" 
            };
            
            var validationResult = new ValidationResult();
            _loginValidatorMock.ValidateAsync(request).Returns(validationResult);

            var loginResponse = new LoginResponse
            {
                IsSuccess = false,
                ErrorMessage = "Invalid credentials."
            };
            _loginServiceMock.LoginWithPassword(request, CancellationToken.None).Returns(loginResponse);

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            var responseBody = unauthorizedResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task LoginWithPassword_WhenCredentialsAreValid_ReturnsToken()
        {
            // Arrange
            var request = new LoginRequest 
            { 
                Username = "validuser", 
                Password = "correctpassword" 
            };
            
            var validationResult = new ValidationResult();
            _loginValidatorMock.ValidateAsync(request).Returns(validationResult);

            var loginResponse = new LoginResponse
            {
                IsSuccess = true,
                Token = "valid-jwt-token",
                RefreshToken = "valid-refresh-token",
                Expiration = DateTime.UtcNow.AddHours(1),
                ErrorMessage = ""
            };
            _loginServiceMock.LoginWithPassword(request, CancellationToken.None).Returns(loginResponse);

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var response = okResult.Value.Should().BeAssignableTo<LoginResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.Token.Should().Be("valid-jwt-token");
            response.RefreshToken.Should().Be("valid-refresh-token");
            response.Expiration.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), new TimeSpan(0,1,0)); // 1000 milliseconds precision
        }

        [Fact]
        public async Task LoginWithPassword_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var request = new LoginRequest 
            { 
                Username = "validuser", 
                Password = "correctpassword" 
            };
            
            var validationResult = new ValidationResult();
            _loginValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.LoginWithPassword(request, CancellationToken.None).ThrowsForAnyArgs(new Exception("Unexpected error"));

            // Act
            var result = await _controller.LoginWithPassword(request);

            // Assert
            var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
            serverErrorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var responseBody = serverErrorResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("An unexpected error occurred");
        }
    }
}
