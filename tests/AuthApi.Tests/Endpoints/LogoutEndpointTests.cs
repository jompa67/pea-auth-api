using System.Security.Claims;
using AuthApi.Controllers;
using AuthApi.Services.Interface;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace AuthApi.Tests.Endpoints
{
    public class LogoutEndpointTests
    {
        private AuthController _controller;
        private ILogger<AuthController> _loggerMock;
        private ILoginService _loginServiceMock;
        private IValidator<AuthApi.Contracts.Login.LoginRequest> _loginValidatorMock;
        private IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest> _registerValidatorMock;
        private IValidator<AuthApi.Contracts.RefreshTokenRequest> _refreshTokenValidatorMock;
        private IVerificationTokenService _verificationTokenServiceMock;

        public LogoutEndpointTests()
        {
            _loggerMock = Substitute.For<ILogger<AuthController>>();
            _loginServiceMock = Substitute.For<ILoginService>();
            _loginValidatorMock = Substitute.For<IValidator<AuthApi.Contracts.Login.LoginRequest>>();
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
        public async Task LogoutUser_WhenUserIsAuthenticated_ReturnsNoContent()
        {
            // Arrange
            var username = "testuser";
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "test");
            var user = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            _loginServiceMock.LogoutUser(username, CancellationToken.None).Returns(true);

            // Act
            var result = await _controller.LogoutUser();

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task LogoutUser_WhenLogoutFails_ReturnsInternalServerError()
        {
            // Arrange
            var username = "testuser";
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "test");
            var user = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            _loginServiceMock.LogoutUser(username, CancellationToken.None).Returns(false);

            // Act
            var result = await _controller.LogoutUser();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("Logout failed");
        }

        [Fact]
        public async Task LogoutUser_WhenUserIsNotAuthenticated_ReturnsInternalServerError()
        {
            // Arrange
            // No authenticated user (User.Identity.Name will be null)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            
            _loginServiceMock.LogoutUser(Arg.Any<string>(), CancellationToken.None).Returns(false);

            // Act
            var result = await _controller.LogoutUser();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }

        [Fact]
        public async Task LogoutUser_WhenExceptionOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var username = "testuser";
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "test");
            var user = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            _loginServiceMock.LogoutUser(username, CancellationToken.None).ThrowsForAnyArgs(new Exception("Unexpected error"));

            // Act
            var result = await _controller.LogoutUser();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var responseBody = statusCodeResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("An unexpected error occurred");
        }
    }
}
