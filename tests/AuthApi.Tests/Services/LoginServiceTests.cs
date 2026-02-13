using System.Net;
using AuthApi.Contracts.Login;
using AuthApi.Contracts.Register;
using AuthApi.Models;
using AuthApi.Models.Enums;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AuthApi.Services.Interface;

namespace AuthApi.Tests.Services
{
    public class LoginServiceTests
    {
        private ILoginService _loginService;
        private ILogger<LoginService> _loggerMock;
        private IJwtTokenGenerator _jwtTokenGeneratorMock;
        private IUserProfileRepository _userProfileRepositoryMock;
        private IUserLoginRepository _userLoginRepositoryMock;
        private IRefreshTokenRepository _refreshTokenRepositoryMock;
        private IVerificationTokenService _verificationTokenServiceMock;
        private IEmailService _emailServiceMock;
        private IBCryptWrapper _bcryptWrapperMock;

        public LoginServiceTests()
        {
            _loggerMock = Substitute.For<ILogger<LoginService>>();
            _jwtTokenGeneratorMock = Substitute.For<IJwtTokenGenerator>();
            _userProfileRepositoryMock = Substitute.For<IUserProfileRepository>();
            _userLoginRepositoryMock = Substitute.For<IUserLoginRepository>();
            _refreshTokenRepositoryMock = Substitute.For<IRefreshTokenRepository>();
            _verificationTokenServiceMock = Substitute.For<IVerificationTokenService>();
            _emailServiceMock = Substitute.For<IEmailService>();
            _bcryptWrapperMock = Substitute.For<IBCryptWrapper>();
        
            _loginService = new LoginService(
                _loggerMock,
                _jwtTokenGeneratorMock,
                _userProfileRepositoryMock,
                _userLoginRepositoryMock,
                _refreshTokenRepositoryMock,
                _emailServiceMock,
                _verificationTokenServiceMock,
                _bcryptWrapperMock
            );
        }

        [Fact]
        public async Task RegisterWithPassword_WhenUsernameExists_ShouldReturnConflictResult()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "existinguser",
                Email = "user@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            _userProfileRepositoryMock.GetByUsernameAsync("existinguser", CancellationToken.None).Returns(new UserProfile
            {
                UserId = Guid.NewGuid(),
                Username = "existinguser",
                Email = "existinguser@example.com",
                UsernameOriginal = "existinguser",
                FirstName = "Existing",
                LastName = "User"
            });

            // Act
            var result = await _loginService.RegisterWithPassword(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            result.Message.Should().Be("Username is already taken");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenEmailExists_ShouldReturnConflictResult()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            _userProfileRepositoryMock.GetByUsernameAsync("newuser", CancellationToken.None).Returns((UserProfile)null);
            _userProfileRepositoryMock.GetByEmailAsync("existing@example.com", CancellationToken.None).Returns(new UserProfile
            {
                UserId = Guid.NewGuid(),
                Username = "existinguser",
                Email = "existing@example.com",
                UsernameOriginal = "existinguser",
                FirstName = "Existing",
                LastName = "User"
            });

            // Act
            var result = await _loginService.RegisterWithPassword(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
            result.Message.Should().Be("Email is already in use");
        }

        [Fact]
        public async Task RegisterWithPassword_WhenValid_ShouldCreateUserAndSendVerificationEmail()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "newuser",
                Email = "new@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };
        
            _userProfileRepositoryMock.GetByUsernameAsync("newuser", CancellationToken.None).Returns((UserProfile)null);
            _userProfileRepositoryMock.GetByEmailAsync("new@example.com", CancellationToken.None).Returns((UserProfile)null);
            _userProfileRepositoryMock.CreateUserProfile(Arg.Any<UserProfile>(),CancellationToken.None).Returns(true);
            _userLoginRepositoryMock.CreateUserLogin(Arg.Any<UserLogin>()).Returns(true);
            _verificationTokenServiceMock.GenerateEmailVerificationTokenAsync(Arg.Any<UserProfile>(),CancellationToken.None).Returns(Task.FromResult("verification-token"));
            _emailServiceMock.SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        
            // Act
            var result = await _loginService.RegisterWithPassword(request, CancellationToken.None);
        
            // Assert
            result.IsSuccess.Should().BeTrue();
            result.StatusCode.Should().Be(StatusCodes.Status201Created);
            result.Message.Should().Contain("registered successfully");
        
            // Verify that the user profile and login were created
            await _userProfileRepositoryMock.Received(1).CreateUserProfile(Arg.Is<UserProfile>(p => 
                p.Username == "newuser" && 
                p.Email == "new@example.com" && 
                p.EmailVerified == false),CancellationToken.None);
            await _userLoginRepositoryMock.Received(1).CreateUserLogin(Arg.Any<UserLogin>());
            
            // Verify that the verification email was sent
            await _verificationTokenServiceMock.Received(1).GenerateEmailVerificationTokenAsync(Arg.Any<UserProfile>(),CancellationToken.None);
            await _emailServiceMock.Received(1).SendVerificationEmailAsync(
                Arg.Is<string>(e => e == "new@example.com"), 
                Arg.Is<string>(u => u == "newuser"), 
                Arg.Any<string>());
        }

        [Fact]
        public async Task LoginWithPassword_WhenUserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "nonexistentuser",
                Password = "Password123!"
            };

            _bcryptWrapperMock.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            _userProfileRepositoryMock.GetByUsernameAsync("nonexistentuser", CancellationToken.None).Returns((UserProfile)null);

            // Act
            var result = await _loginService.LoginWithPassword(request,CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task LoginWithPassword_WhenPasswordInvalid_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new LoginRequest
            {
                Username = "existinguser",
                Password = "WrongPassword!"
            };

            var userProfile = new UserProfile
            {
                UserId = userId,
                Username = "existinguser",
                Email = "existinguser@example.com",
                UsernameOriginal = "existinguser",
                FirstName = "Existing",
                LastName = "User",
                EmailVerified = true
            };

            var userLogin = new UserLogin
            {
                UserId = userId,
                AuthProvider = AuthProvider.Password,
                AuthType = AuthType.Password,
                AuthValue = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
            };

            _userProfileRepositoryMock.GetByUsernameAsync("existinguser",CancellationToken.None).Returns(userProfile);
            _userLoginRepositoryMock.GetByUserIdAndProviderAsync(userId, AuthProvider.Password).Returns(userLogin);

            // Act
            var result = await _loginService.LoginWithPassword(request,CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid credentials.");
        }

        [Fact]
        public async Task GetRefreshToken_WhenTokenNull_ShouldReturnBadRequest()
        {
            // Arrange
            string? token = null;
            string refreshToken = "validRefreshToken";

            // Act
            var result = await _loginService.GetRefreshToken(token, refreshToken, CancellationToken.None);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Be("Token and refresh token are required.");
            result.Response.Should().BeNull();
        }

        [Fact]
        public async Task GetRefreshToken_WhenRefreshTokenNull_ShouldReturnBadRequest()
        {
            // Arrange
            string token = "validToken";
            string refreshToken = null;

            // Act
            var result = await _loginService.GetRefreshToken(token, refreshToken, CancellationToken.None);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Be("Token and refresh token are required.");
            result.Response.Should().BeNull();
        }
    }
}
