using System.Security.Claims;
using AuthApi.Controllers;
using AuthApi.Services.Interface;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AuthApi.Tests.Endpoints
{
    public class ProtectedEndpointTests
    {
        private AuthController _controller;
        private ILogger<AuthController> _loggerMock;
        private ILoginService _loginServiceMock;
        private IValidator<AuthApi.Contracts.Login.LoginRequest> _loginValidatorMock;
        private IValidator<AuthApi.Contracts.Register.RegisterWithPasswordRequest> _registerValidatorMock;
        private IValidator<AuthApi.Contracts.RefreshTokenRequest> _refreshTokenValidatorMock;
        private IVerificationTokenService _verificationTokenServiceMock;

        public ProtectedEndpointTests()
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
        public void ProtectedEndpoint_WhenUserIsAuthenticated_ReturnsOkWithMessage()
        {
            // Arrange
            var username = "authenticateduser";
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "test");
            var user = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = _controller.ProtectedEndpoint();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var responseBody = okResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("protected endpoint");
        }

        [Fact]
        public void ProtectedEndpoint_WhenUserIsNotAuthenticated_StillWorksButWithNullUsername()
        {
            // Arrange - No authenticated user (User.Identity.Name will be null)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = _controller.ProtectedEndpoint();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            var responseBody = okResult.Value.Should().BeAssignableTo<object>().Subject;
            responseBody.ToString().Should().Contain("protected endpoint");
        }
    }
}
