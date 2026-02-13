using Microsoft.Extensions.Options;

namespace AuthApi.Settings;

/// <summary>
/// Diagnostic tool to verify JWT settings configuration is working correctly
/// </summary>
public class JwtSettingsConfigurerDiagnostics : IHostedService
{
    private readonly ILogger<JwtSettingsConfigurerDiagnostics> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly IHostEnvironment _environment;

    public JwtSettingsConfigurerDiagnostics(
        ILogger<JwtSettingsConfigurerDiagnostics> logger,
        IOptions<JwtSettings> jwtSettings,
        IHostEnvironment environment)
    {
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Only log in development to avoid leaking sensitive data in production
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("JWT Settings Configuration Check on Startup");
            _logger.LogInformation("Issuer: {Issuer}", _jwtSettings.Issuer ?? "[not set]");
            _logger.LogInformation("Audience: {Audience}", _jwtSettings.Audience ?? "[not set]");
            _logger.LogInformation("PublicKey: {PublicKeyPresent}", !string.IsNullOrEmpty(_jwtSettings.PublicKey) ? "[present]" : "[missing]");
            _logger.LogInformation("PrivateKey: {PrivateKeyPresent}", !string.IsNullOrEmpty(_jwtSettings.PrivateKey) ? "[present]" : "[missing]");
            _logger.LogInformation("ExpirationMinutes: {ExpirationMinutes}", _jwtSettings.ExpirationMinutes);
            _logger.LogInformation("RefreshTokenExpirationDays: {RefreshTokenExpirationDays}", _jwtSettings.RefreshTokenExpirationDays);
            _logger.LogInformation("ValidateLifetime: {ValidateLifetime}", _jwtSettings.ValidateLifetime);
            _logger.LogInformation("ClockSkewMinutes: {ClockSkewMinutes}", _jwtSettings.ClockSkewMinutes);

            // Check environment variables 
            _logger.LogInformation("Environment Variables Check:");
            _logger.LogInformation("JwtSettings__Issuer: {Value}", Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? "[not set]");
            _logger.LogInformation("JwtSettings__Audience: {Value}", Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? "[not set]");
            _logger.LogInformation("JwtSettings__PublicKey: {Value}", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JwtSettings__PublicKey")) ? "[present]" : "[missing]");
            _logger.LogInformation("JwtSettings__PrivateKey: {Value}", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JwtSettings__PrivateKey")) ? "[present]" : "[missing]");
            _logger.LogInformation("JwtSettings__ExpirationMinutes: {Value}", Environment.GetEnvironmentVariable("JwtSettings__ExpirationMinutes") ?? "[not set]");
            _logger.LogInformation("JwtSettings__RefreshTokenExpirationDays: {Value}", Environment.GetEnvironmentVariable("JwtSettings__RefreshTokenExpirationDays") ?? "[not set]");
            _logger.LogInformation("JwtSettings__ValidateLifetime: {Value}", Environment.GetEnvironmentVariable("JwtSettings__ValidateLifetime") ?? "[not set]");
            _logger.LogInformation("JwtSettings__ClockSkewMinutes: {Value}", Environment.GetEnvironmentVariable("JwtSettings__ClockSkewMinutes") ?? "[not set]");
        }
        else
        {
            // Simple check in production without exposing values
            _logger.LogInformation("JWT Settings loaded successfully with Issuer: {Issuer}", _jwtSettings.Issuer);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
