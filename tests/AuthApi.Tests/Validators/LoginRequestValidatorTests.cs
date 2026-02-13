using AuthApi.Contracts.Login;
using AuthApi.Settings;
using AuthApi.Validators;
using Microsoft.Extensions.Options;

namespace AuthApi.Tests.Validators
{
    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator _validator;
        private readonly ValidationSettings _validationSettings;
        private readonly IOptions<ValidationSettings> _mockOptions;

        public LoginRequestValidatorTests()
        {
            // Create default validation settings
            _validationSettings = new ValidationSettings
            {
                Password = new ValidationSettings.PasswordValidationSettings
                {
                    MinimumLength = 8,
                    MaximumLength = 100,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireNonAlphanumeric = true
                },
                User = new ValidationSettings.UserValidationSettings
                {
                    UsernameMinLength = 3,
                    UsernameMaxLength = 50,
                    EmailRegexPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
                }
            };

            // Setup mock Options using NSubstitute
            _mockOptions = Substitute.For<IOptions<ValidationSettings>>();
            _mockOptions.Value.Returns(_validationSettings);

            // Create validator with mock options
            _validator = new LoginRequestValidator(_mockOptions);
        }

        [Fact]
        public void Validate_WhenUsernameIsNull_ShouldHaveError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = null,
                Password = "Password123!"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Username");
        }

        [Fact]
        public void Validate_WhenUsernameIsTooShort_ShouldHaveError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "ab", // Less than minimum length of 3
                Password = "Password123!"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Username");
        }

        [Fact]
        public void Validate_WhenUsernameIsTooLong_ShouldHaveError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = new string('a', 51), // More than maximum length of 50
                Password = "Password123!"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Username");
        }

        [Fact]
        public void Validate_WhenPasswordIsNull_ShouldHaveError()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "validuser",
                Password = null
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }

        [Fact]
        public void Validate_WhenPasswordIsTooShort_ShouldHaveError()
        {
            // Arrange - Set minimum password length for this test
            _validationSettings.Password.MinimumLength = 8;
            _mockOptions.Value.Returns(_validationSettings);
            
            var validator = new LoginRequestValidator(_mockOptions);
            
            var request = new LoginRequest
            {
                Username = "validuser",
                Password = "short" // Less than minimum length of 8
            };

            // Act
            var result = validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                               e.ErrorMessage.Contains("at least 8"));
        }

        [Fact]
        public void Validate_WhenRequestIsValid_ShouldNotHaveErrors()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "validuser",
                Password = "ValidPassword123!"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        
        [Fact]
        public void Validate_WithCustomConfiguration_ShouldApplyCustomRules()
        {
            // Arrange - Create custom validation settings with shorter requirements
            var customSettings = new ValidationSettings
            {
                Password = new ValidationSettings.PasswordValidationSettings
                {
                    MinimumLength = 3,
                    MaximumLength = 30
                },
                User = new ValidationSettings.UserValidationSettings
                {
                    UsernameMinLength = 2,
                    UsernameMaxLength = 20
                }
            };

            // Create a new mock with the custom settings using NSubstitute
            var customMockOptions = Substitute.For<IOptions<ValidationSettings>>();
            customMockOptions.Value.Returns(customSettings);
            
            // Create a new validator with the custom settings
            var customValidator = new LoginRequestValidator(customMockOptions);
            
            // Create a request that would fail with default settings but pass with custom settings
            var request = new LoginRequest
            {
                Username = "ab", // Only 2 characters - would fail with default
                Password = "pwd" // Only 3 characters - would fail with default
            };

            // Act
            var result = customValidator.Validate(request);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
