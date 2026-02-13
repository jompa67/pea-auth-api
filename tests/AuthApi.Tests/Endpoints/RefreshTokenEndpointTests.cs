using System.Net;
using AuthApi.Contracts;
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
    public class RefreshTokenEndpointTests
    {
        private AuthController _controller;
        private ILogger<AuthController> _loggerMock;
        private ILoginService _loginServiceMock;
        private IValidator<LoginRequest> _loginValidatorMock;
        private IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest> _registerValidatorMock;
        private IValidator<RefreshTokenRequest> _refreshTokenValidatorMock;
        private IVerificationTokenService _verificationTokenServiceMock;

        public RefreshTokenEndpointTests()
        {
            _loggerMock = Substitute.For<ILogger<AuthController>>();
            _loginServiceMock = Substitute.For<ILoginService>();
            _loginValidatorMock = Substitute.For<IValidator<LoginRequest>>();
            _registerValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest>>();
            _refreshTokenValidatorMock = Substitute.For<IValidator<RefreshTokenRequest>>();
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
        public async Task RefreshToken_WhenValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "",
                RefreshToken = ""
            };
            
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Token", "Token is required"),
                new ValidationFailure("RefreshToken", "Refresh token is required")
            };
            
            var validationResult = new ValidationResult(validationFailures);
            _refreshTokenValidatorMock.ValidateAsync(request).Returns(validationResult);

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errors = badRequestResult.Value.Should().BeAssignableTo<Dictionary<string, string>>().Subject;
            errors.Should().ContainKey("Token");
            errors.Should().ContainKey("RefreshToken");
        }

        [Fact]
        public async Task RefreshToken_WhenTokenIsInvalid_ReturnsForbidden()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "invalid-token",
                RefreshToken = "invalid-refresh-token"
            };
            
            var validationResult = new ValidationResult();
            _refreshTokenValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.GetRefreshToken(request.Token, request.RefreshToken, CancellationToken.None)
                .Returns((null, HttpStatusCode.Forbidden, "Invalid refresh token."));

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Invalid refresh token");
        }

        [Fact]
        public async Task RefreshToken_WhenTokenIsExpired_ReturnsForbidden()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "expired-token",
                RefreshToken = "expired-refresh-token"
            };
            
            var validationResult = new ValidationResult();
            _refreshTokenValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.GetRefreshToken(request.Token, request.RefreshToken, CancellationToken.None)
                .Returns((null, HttpStatusCode.Forbidden, "Refresh token has expired."));

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Refresh token has expired");
        }

        [Fact]
        public async Task RefreshToken_WhenTokenIsUsedOrRevoked_ReturnsForbidden()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "used-token",
                RefreshToken = "used-refresh-token"
            };
            
            var validationResult = new ValidationResult();
            _refreshTokenValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.GetRefreshToken(request.Token, request.RefreshToken, CancellationToken.None)
                .Returns((null, HttpStatusCode.Forbidden, "Refresh token has been used or revoked."));

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Refresh token has been used or revoked");
        }

        [Fact]
        public async Task RefreshToken_WhenTokenIsValid_ReturnsNewTokens()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "valid-token",
                RefreshToken = "valid-refresh-token"
            };
            
            var validationResult = new ValidationResult();
            _refreshTokenValidatorMock.ValidateAsync(request).Returns(validationResult);

            var loginResponse = new LoginResponse
            {
                IsSuccess = true,
                Token = "new-token",
                RefreshToken = "new-refresh-token",
                Expiration = DateTime.UtcNow.AddHours(1),
                ErrorMessage = ""
            };

            _loginServiceMock.GetRefreshToken(request.Token, request.RefreshToken, CancellationToken.None)
                .Returns((loginResponse, HttpStatusCode.OK, "Token refreshed successfully"));

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var response = okResult.Value.Should().BeAssignableTo<LoginResponse>().Subject;
            response.IsSuccess.Should().BeTrue();
            response.Token.Should().Be("new-token");
            response.RefreshToken.Should().Be("new-refresh-token");
        }

        [Fact]
        public async Task RefreshToken_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "valid-token",
                RefreshToken = "valid-refresh-token"
            };
            
            var validationResult = new ValidationResult();
            _refreshTokenValidatorMock.ValidateAsync(request).Returns(validationResult);

            _loginServiceMock.GetRefreshToken(request.Token, request.RefreshToken, CancellationToken.None)
                .ThrowsForAnyArgs(new Exception("Unexpected error"));

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var serverErrorResult = result.Should().BeOfType<ObjectResult>().Subject;
            serverErrorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var responseBody = serverErrorResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("An unexpected error occurred");
        }
    }
}
