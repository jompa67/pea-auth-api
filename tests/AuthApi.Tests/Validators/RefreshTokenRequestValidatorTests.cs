using AuthApi.Contracts;
using AuthApi.Settings;
using AuthApi.Validators;
using Microsoft.Extensions.Options;

namespace AuthApi.Tests.Validators
{
    public class RefreshTokenRequestValidatorTests
    {
        private RefreshTokenRequestValidator _validator;
        private ValidationSettings _validationSettings;
        private IOptions<ValidationSettings> _mockOptions;

        public RefreshTokenRequestValidatorTests()
        {
            // Create default validation settings
            _validationSettings = new ValidationSettings();

            // Setup mock Options
            _mockOptions = Substitute.For<IOptions<ValidationSettings>>();
            _mockOptions.Value.Returns(_validationSettings);

            // Create validator with mock options
            _validator = new RefreshTokenRequestValidator(_mockOptions);
        }

        [Fact]
        public void Validate_WhenTokenIsNull_ShouldHaveError()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = null,
                RefreshToken = "valid-refresh-token"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Token");
        }

        [Fact]
        public void Validate_WhenTokenIsEmpty_ShouldHaveError()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = string.Empty,
                RefreshToken = "valid-refresh-token"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Token");
        }

        [Fact]
        public void Validate_WhenRefreshTokenIsNull_ShouldHaveError()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "valid-token",
                RefreshToken = null
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
        }

        [Fact]
        public void Validate_WhenRefreshTokenIsEmpty_ShouldHaveError()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "valid-token",
                RefreshToken = string.Empty
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
        }

        [Fact]
        public void Validate_WhenRequestIsValid_ShouldNotHaveErrors()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                Token = "valid-token",
                RefreshToken = "valid-refresh-token"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
