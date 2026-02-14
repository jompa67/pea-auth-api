using AuthApi.Attributes;

namespace AuthApi.Settings;

public class JwtSettings
{
    [KmsDecryption] public string PrivateKey { get; set; } = string.Empty;
    public string PrivateKeySecret { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
    
    // Additional settings from appsettings.json
    public int RefreshTokenExpirationDays { get; set; } = 30; // Default value
    public bool ValidateLifetime { get; set; } = true; // Default value 
    public int ClockSkewMinutes { get; set; } = 5; // Default value
}