using AuthApi.Contracts.Register;
using AuthApi.Controllers;
using AuthApi.Services;
using AuthApi.Services.Interface;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AuthApi.Tests.Endpoints
{
    public class RegisterEndpointTests
    {
        private AuthController _controller;
        private ILogger<AuthController> _loggerMock;
        private ILoginService _loginServiceMock;
        private IValidator<RegisterWithPasswordRequest> _registerValidatorMock;
        private IValidator<AuthApi.Contracts.Login.LoginRequest> _loginValidatorMock;
        private IValidator<AuthApi.Contracts.RefreshTokenRequest> _refreshTokenValidatorMock;
        private IVerificationTokenService _verificationTokenServiceMock;

        public RegisterEndpointTests()
        {
            _loggerMock = Substitute.For<ILogger<AuthController>>();
            _loginServiceMock = Substitute.For<ILoginService>();
            _registerValidatorMock = Substitute.For<IValidator<RegisterWithPasswordRequest>>();
            _loginValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.Login.LoginRequest>>();
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
        public async Task RegisterWithPassword_WhenValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "",
                Password = "",
                Email = "invalid-email",
                FirstName = "",
                LastName = ""
            };

            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Username", "Username is required"),
                new ValidationFailure("Password", "Password is required"),
                new ValidationFailure("Email", "Email is not valid"),
                new ValidationFailure("FirstName", "First name is required"),
                new ValidationFailure("LastName", "Last name is required")
            };
            
            var validationResult = new ValidationResult(validationFailures);
            _registerValidatorMock.ValidateAsync(request).Returns(validationResult);

            // Act
            var result = await _controller.RegisterWithPassword(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errors = badRequestResult.Value.Should().BeAssignableTo<Dictionary<string, string>>().Subject;
            errors.Should().ContainKey("Username");
            errors.Should().ContainKey("Password");
            errors.Should().ContainKey("Email");
            errors.Should().ContainKey("FirstName");
            errors.Should().ContainKey("LastName");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenUsernameExists_ReturnsConflict()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "existinguser",
                Password = "Password123!",
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe"
            };
            
            var validationResult = new ValidationResult();
            _registerValidatorMock.ValidateAsync(request).Returns(validationResult);

            var serviceResult = new RegisterResult
            {
                IsSuccess = false,
                Message = "Username is already taken",
                StatusCode = StatusCodes.Status409Conflict
            };
            _loginServiceMock.RegisterWithPassword(request, CancellationToken.None).Returns(serviceResult);

            // Act
            var result = await _controller.RegisterWithPassword(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Username is already taken");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenEmailExists_ReturnsConflict()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "newuser",
                Password = "Password123!",
                Email = "existing@example.com",
                FirstName = "John",
                LastName = "Doe"
            };
            
            var validationResult = new ValidationResult();
            _registerValidatorMock.ValidateAsync(request).Returns(validationResult);

            var serviceResult = new RegisterResult
            {
                IsSuccess = false,
                Message = "Email is already in use",
                StatusCode = StatusCodes.Status409Conflict
            };
            _loginServiceMock.RegisterWithPassword(request, CancellationToken.None).Returns(serviceResult);

            // Act
            var result = await _controller.RegisterWithPassword(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Email is already in use");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenSuccessful_ReturnsCreatedResult()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "newuser",
                Password = "Password123!",
                Email = "new@example.com",
                FirstName = "John",
                LastName = "Doe"
            };
            
            var validationResult = new ValidationResult();
            _registerValidatorMock.ValidateAsync(request).Returns(validationResult);

            var serviceResult = new RegisterResult
            {
                IsSuccess = true,
                Message = "User registered successfully",
                StatusCode = StatusCodes.Status201Created
            };
            _loginServiceMock.RegisterWithPassword(request, CancellationToken.None).Returns(serviceResult);

            // Act
            var result = await _controller.RegisterWithPassword(request);

            // Assert
            var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
            createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
            var responseBody = createdResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("User registered successfully");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "newuser",
                Password = "Password123!",
                Email = "new@example.com",
                FirstName = "John",
                LastName = "Doe"
            };
            
            var validationResult = new ValidationResult();
            _registerValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.RegisterWithPassword(request, CancellationToken.None).ThrowsForAnyArgs(new InvalidOperationException("Test exception"));

            // Act
            var result = await _controller.RegisterWithPassword(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            var responseBody = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Registration failed");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenUnexpectedExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "newuser",
                Password = "Password123!",
                Email = "new@example.com",
                FirstName = "John",
                LastName = "Doe"
            };
            
            var validationResult = new ValidationResult();
            _registerValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.RegisterWithPassword(request, CancellationToken.None).ThrowsForAnyArgs(new Exception("Unexpected error"));

            // Act
            var result = await _controller.RegisterWithPassword(request);

            // Assert
            var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
            serverErrorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var responseBody = serverErrorResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("An unexpected error occurred");
        }
    }
}
