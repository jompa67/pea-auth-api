using Microsoft.Extensions.Options;

namespace AuthApi.Settings;

/// <summary>
/// Configures settings from environment variables, overriding configuration values
/// </summary>
/// <typeparam name="TOptions">The settings class type</typeparam>
public class EnvironmentSettingsConfigurer<TOptions> : IConfigureOptions<TOptions> where TOptions : class
{
    private readonly IConfiguration _configuration;

    public EnvironmentSettingsConfigurer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(TOptions options)
    {
        // For each property, check if there's an environment variable and apply it
        var properties = typeof(TOptions).GetProperties()
            .Where(p => p.CanWrite && (p.PropertyType == typeof(string) || p.PropertyType == typeof(string)));

        foreach (var property in properties)
        {
            var envKey = $"{typeof(TOptions).Name}__{property.Name}";
            var envValue = Environment.GetEnvironmentVariable(envKey);

            if (!string.IsNullOrEmpty(envValue))
            {
                property.SetValue(options, envValue);
            }
        }
    }
}
