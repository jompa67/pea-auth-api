using AuthApi.Contracts.Register;
using AuthApi.Services.Interface;
using AuthApi.Settings;
using AuthApi.Validators;
using Microsoft.Extensions.Options;

namespace AuthApi.Tests.Validators
{
    public class RegisterWithPasswordRequestValidatorTests
    {
        private RegisterWithPasswordRequestValidator _validator;
        private ValidationSettings _validationSettings;
        private IOptions<ValidationSettings> _mockOptions;
        private IBCryptWrapper _bcryptWrapperMock;

        public RegisterWithPasswordRequestValidatorTests()
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

            // Setup mock Options with NSubstitute
            _mockOptions = Substitute.For<IOptions<ValidationSettings>>();
            _mockOptions.Value.Returns(_validationSettings);
            
            // Setup BCrypt wrapper mock
            _bcryptWrapperMock = Substitute.For<IBCryptWrapper>();

            // Create validator with mock options
            _validator = new RegisterWithPasswordRequestValidator(_mockOptions);
        }

        [Fact]
        public void Validate_WhenUsernameIsTooShort_ShouldHaveError()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "ab", // Shorter than minimum length
                Email = "valid@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };
        
            // Act
            var result = _validator.Validate(request);
        
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Username");
        }
        
        [Fact]
        public void Validate_WhenEmailIsInvalid_ShouldHaveError()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "invalid-email", // Not a valid email format
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Fact]
        public void Validate_WhenPasswordDoesNotMeetComplexityRequirements_ShouldHaveError()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "simplepassword", // Missing uppercase, numbers, special characters
                FirstName = "John",
                LastName = "Doe"
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
            // Arrange - Explicitly set minimum password length for this test
            _validationSettings.Password.MinimumLength = 8;
            _mockOptions.Value.Returns(_validationSettings);
            
            var validator = new RegisterWithPasswordRequestValidator(_mockOptions);
            
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "Pw1!", // 4 characters - less than minimum 8
                FirstName = "John",
                LastName = "Doe"
            };
        
            // Act
            var result = validator.Validate(request);
        
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                               e.ErrorMessage.Contains("between 8 and"));
        }
        
        [Fact]
        public void Validate_WhenFirstNameIsEmpty_ShouldHaveError()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "Password123!",
                FirstName = "",
                LastName = "Doe"
            };

            // Act
            var result = _validator.Validate(request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
        }

        [Fact]
        public void Validate_WhenLastNameIsEmpty_ShouldHaveError()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = ""
            };
        
            // Act
            var result = _validator.Validate(request);
        
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "LastName");
        }
        
        [Fact]
        public void Validate_WhenRequestIsValid_ShouldNotHaveErrors()
        {
            // Arrange
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
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
            // Arrange - Create custom validation settings
            var customSettings = new ValidationSettings
            {
                Password = new ValidationSettings.PasswordValidationSettings
                {
                    MinimumLength = 3,
                    MaximumLength = 30,
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireUppercase = false,
                    RequireNonAlphanumeric = false
                },
                User = new ValidationSettings.UserValidationSettings
                {
                    UsernameMinLength = 2,
                    UsernameMaxLength = 20,
                    EmailRegexPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
                }
            };
        
            // Create a new mock with the custom settings
            var customMockOptions = Substitute.For<IOptions<ValidationSettings>>();
            customMockOptions.Value.Returns(customSettings);
            
            // Create a new validator with the custom settings
            var customValidator = new RegisterWithPasswordRequestValidator(customMockOptions);
            
            // Create a request that would fail with default settings but pass with custom settings
            var request = new RegisterWithPasswordRequest
            {
                Username = "ab", // Only 2 characters - would fail with default
                Email = "valid@example.com",
                Password = "pwd", // Simple password - would fail with default
                FirstName = "John",
                LastName = "Doe"
            };
        
            // Act
            var result = customValidator.Validate(request);
        
            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        
        [Fact]
        public void Validate_WithRequireUppercaseEnabled_ShouldRequireUppercaseLetter()
        {
            // Arrange - Enable RequireUppercase
            _validationSettings.Password.RequireUppercase = true;
            _mockOptions.Value.Returns(_validationSettings);
            
            var validator = new RegisterWithPasswordRequestValidator(_mockOptions);
            
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "password123!", // No uppercase letter
                FirstName = "John",
                LastName = "Doe"
            };
        
            // Act
            var result = validator.Validate(request);
        
            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                                e.ErrorMessage.Contains("uppercase"));
        }
        
        [Fact(Skip = "CHECK THIS")]
        public void Validate_WithRequireUppercaseDisabled_ShouldNotRequireUppercaseLetter()
        {
            // Arrange - Disable all complexity requirements except lowercase (which our test password satisfies)
            _validationSettings.Password.RequireUppercase = false;
            _validationSettings.Password.RequireDigit = false; 
            _validationSettings.Password.RequireNonAlphanumeric = false;
            _mockOptions.Value.Returns(_validationSettings);
            
            var validator = new RegisterWithPasswordRequestValidator(_mockOptions);
            
            var request = new RegisterWithPasswordRequest
            {
                Username = "validuser",
                Email = "valid@example.com",
                Password = "password", // No uppercase, digit, or special character - should pass with these settings
                FirstName = "John",
                LastName = "Doe"
            };
        
            // Act
            var result = validator.Validate(request);
        
            // Assert - Should be valid since we've disabled the requirements it doesn't meet
            result.IsValid.Should().BeTrue();
            result.Errors.Should().NotContain(e => e.PropertyName == "Password" && 
                                                    e.ErrorMessage.Contains("uppercase"));
        }
        
        [Fact]
        public void Validate_PasswordComplexityRules_ShouldBeAppliedIndependently()
        {
            // Test each rule independently to ensure they work correctly
            
            // 1. Test RequireUppercase only
            _validationSettings.Password.RequireUppercase = true;
            _validationSettings.Password.RequireDigit = false;
            _validationSettings.Password.RequireLowercase = false;
            _validationSettings.Password.RequireNonAlphanumeric = false;
            _mockOptions.Value.Returns(_validationSettings);
            
            var validator = new RegisterWithPasswordRequestValidator(_mockOptions);
            
            // Password without uppercase - should fail
            var request1 = new RegisterWithPasswordRequest 
            { 
                Username = "validuser", Email = "valid@example.com", 
                Password = "password", FirstName = "John", LastName = "Doe" 
            };
            validator.Validate(request1).IsValid.Should().BeFalse();
            
            // Password with uppercase - should pass
            var request2 = new RegisterWithPasswordRequest 
            { 
                Username = "validuser", Email = "valid@example.com", 
                Password = "Password", FirstName = "John", LastName = "Doe" 
            };
            validator.Validate(request2).IsValid.Should().BeTrue();
            
            // 2. Test RequireDigit only
            _validationSettings.Password.RequireUppercase = false;
            _validationSettings.Password.RequireDigit = true;
            _validationSettings.Password.RequireLowercase = false;
            _validationSettings.Password.RequireNonAlphanumeric = false;
            _mockOptions.Value.Returns(_validationSettings);
            
            validator = new RegisterWithPasswordRequestValidator(_mockOptions);
            
            // Password without digit - should fail
            var request3 = new RegisterWithPasswordRequest 
            { 
                Username = "validuser", Email = "valid@example.com", 
                Password = "password", FirstName = "John", LastName = "Doe" 
            };
            validator.Validate(request3).IsValid.Should().BeFalse();
            
            // Password with digit - should pass
            var request4 = new RegisterWithPasswordRequest 
            { 
                Username = "validuser", Email = "valid@example.com", 
                Password = "password1", FirstName = "John", LastName = "Doe" 
            };
            validator.Validate(request4).IsValid.Should().BeTrue();
            
            // 3. Test RequireNonAlphanumeric only
            _validationSettings.Password.RequireUppercase = false;
            _validationSettings.Password.RequireDigit = false;
            _validationSettings.Password.RequireLowercase = false;
            _validationSettings.Password.RequireNonAlphanumeric = true;
            _mockOptions.Value.Returns(_validationSettings);
            
            validator = new RegisterWithPasswordRequestValidator(_mockOptions);
            
            // Password without special character - should fail
            var request5 = new RegisterWithPasswordRequest 
            { 
                Username = "validuser", Email = "valid@example.com", 
                Password = "password1", FirstName = "John", LastName = "Doe" 
            };
            validator.Validate(request5).IsValid.Should().BeFalse();
            
            // Password with special character - should pass
            var request6 = new RegisterWithPasswordRequest 
            { 
                Username = "validuser", Email = "valid@example.com", 
                Password = "password!", FirstName = "John", LastName = "Doe" 
            };
            validator.Validate(request6).IsValid.Should().BeTrue();
        }
    }
}
