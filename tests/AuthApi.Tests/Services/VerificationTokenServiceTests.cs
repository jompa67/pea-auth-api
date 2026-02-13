using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services;
using AuthApi.Services.Interface;

namespace AuthApi.Tests.Services
{
    public class VerificationTokenServiceTests
    {
        private IVerificationTokenService _verificationTokenService;
        private IUserProfileRepository _userProfileRepositoryMock;

        public VerificationTokenServiceTests()
        {
            _userProfileRepositoryMock = Substitute.For<IUserProfileRepository>();
            _verificationTokenService = new VerificationTokenService(_userProfileRepositoryMock);
        }

        [Fact]
        public async Task GenerateEmailVerificationToken_ShouldGenerateTokenAndSaveToUserProfile()
        {
            // Arrange
            var userProfile = new UserProfile
            {
                UserId = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com"
            };
            
            _userProfileRepositoryMock.UpdateAsync(Arg.Any<UserProfile>(),CancellationToken.None).Returns(true);

            // Act
            var token = await _verificationTokenService.GenerateEmailVerificationTokenAsync(userProfile, CancellationToken.None);

            // Assert
            token.Should().NotBeNullOrEmpty();
            userProfile.EmailVerificationToken.Should().Be(token);
            userProfile.EmailVerificationTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task ValidateEmailVerificationToken_WithValidToken_ShouldMarkUserAsVerified()
        {
            // Arrange
            var token = "valid-verification-token";
            var expiry = DateTime.UtcNow.AddHours(24);
            
            var userProfile = new UserProfile
            {
                UserId = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                EmailVerificationToken = token,
                EmailVerificationTokenExpiry = expiry,
                EmailVerified = false
            };
            
            var userProfiles = new List<UserProfile> { userProfile };
            _userProfileRepositoryMock.GetAllAsync(CancellationToken.None).Returns(userProfiles);
            _userProfileRepositoryMock.UpdateAsync(Arg.Any<UserProfile>(), CancellationToken.None).Returns(true);

            // Act
            var result = await _verificationTokenService.ValidateEmailVerificationTokenAsync(token, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.EmailVerified.Should().BeTrue();
            result.EmailVerificationToken.Should().BeNull();
            result.EmailVerificationTokenExpiry.Should().BeNull();
            
            await _userProfileRepositoryMock.Received(1).UpdateAsync(userProfile, CancellationToken.None);
        }

        [Fact]
        public async Task ValidateEmailVerificationToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var token = "invalid-token";
            var userProfiles = new List<UserProfile>(); // Empty list, no matching token
            
            _userProfileRepositoryMock.GetAllAsync(CancellationToken.None).Returns(userProfiles);

            // Act
            var result = await _verificationTokenService.ValidateEmailVerificationTokenAsync(token,CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            await _userProfileRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<UserProfile>(), CancellationToken.None);
        }

        [Fact]
        public async Task ValidateEmailVerificationToken_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange
            var token = "expired-token";
            var expiry = DateTime.UtcNow.AddHours(-1); // Expired 1 hour ago
            
            var userProfile = new UserProfile
            {
                UserId = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                EmailVerificationToken = token,
                EmailVerificationTokenExpiry = expiry,
                EmailVerified = false
            };
            
            var userProfiles = new List<UserProfile> { userProfile };
            _userProfileRepositoryMock.GetAllAsync(CancellationToken.None).Returns(userProfiles);

            // Act
            var result = await _verificationTokenService.ValidateEmailVerificationTokenAsync(token, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            await _userProfileRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<UserProfile>(), CancellationToken.None);
        }
    }
}
