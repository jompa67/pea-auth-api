using AuthApi.Services;
using Microsoft.Extensions.Options;

namespace AuthApi.Settings;

/// <summary>
/// Configures JwtSettings with values from environment variables, with proper fallbacks
/// </summary>
public class JwtSettingsConfigurer : IConfigureOptions<JwtSettings>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtSettingsConfigurer> _logger;
    private readonly ISecretsManagerService? _secretsManager;

    public JwtSettingsConfigurer(IConfiguration configuration, ILogger<JwtSettingsConfigurer> logger, ISecretsManagerService? secretsManager = null)
    {
        _configuration = configuration;
        _logger = logger;
        _secretsManager = secretsManager;
    }

    public void Configure(JwtSettings options)
    {
        _logger.LogInformation("Configuring JWT settings from environment variables");

        // Check environment variables first (AWS Lambda style variable naming)
        var privateKeySecretArn = GetValueFromEnvironmentOrConfig("JwtSettings__PrivateKeySecret", string.Empty);

        if (!string.IsNullOrEmpty(privateKeySecretArn) && _secretsManager != null)
        {
            _logger.LogInformation("Fetching JWT private key from Secrets Manager");
            options.PrivateKey = _secretsManager.GetSecretAsync(privateKeySecretArn).GetAwaiter().GetResult();
        }
        else
        {
            _logger.LogWarning("No PrivateKeySecret ARN found, falling back to direct PrivateKey environment variable");
            options.PrivateKey = GetValueFromEnvironmentOrConfig("JwtSettings__PrivateKey", options.PrivateKey);
        }

        options.PublicKey = GetValueFromEnvironmentOrConfig("JwtSettings__PublicKey", options.PublicKey);
        options.Issuer = GetValueFromEnvironmentOrConfig("JwtSettings__Issuer", options.Issuer);
        options.Audience = GetValueFromEnvironmentOrConfig("JwtSettings__Audience", options.Audience);

        // Log the settings (masking sensitive values)
        _logger.LogInformation("JWT settings configured: Issuer={Issuer}, Audience={Audience}, PublicKey={PublicKeyPresent}, PrivateKey={PrivateKeyPresent}",
            options.Issuer,
            options.Audience,
            !string.IsNullOrEmpty(options.PublicKey) ? "[PRESENT]" : "[MISSING]",
            !string.IsNullOrEmpty(options.PrivateKey) ? "[PRESENT]" : "[MISSING]");
    }
    
    private string GetValueFromEnvironmentOrConfig(string key, string defaultValue)
    {
        // Try environment variable first
        var envValue = Environment.GetEnvironmentVariable(key);
        
        if (!string.IsNullOrEmpty(envValue))
        {
            _logger.LogDebug("Using environment variable value for {Key}", key);
            return envValue;
        }
        
        // Then try configuration (which might also come from environment variables via the configuration providers)
        var configValue = _configuration[key];
        
        if (!string.IsNullOrEmpty(configValue))
        {
            _logger.LogDebug("Using configuration value for {Key}", key);
            return configValue;
        }
        
        // Fall back to the default value
        _logger.LogDebug("Using default value for {Key}", key);
        return defaultValue;
    }
}
