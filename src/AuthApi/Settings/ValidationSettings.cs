using System;

namespace AuthApi.Settings;

public class ValidationSettings
{
    public PasswordValidationSettings Password { get; set; } = new();
    public UserValidationSettings User { get; set; } = new();
    
    public class PasswordValidationSettings
    {
        public int MinimumLength { get; set; } = 8;
        public int MaximumLength { get; set; } = 100;
        public bool RequireDigit { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireNonAlphanumeric { get; set; } = true;
    }
    
    public class UserValidationSettings
    {
        public int UsernameMinLength { get; set; } = 3;
        public int UsernameMaxLength { get; set; } = 50;
        public string EmailRegexPattern { get; set; } = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    }
}
