using System.Reflection;
using System.Text;
using System.Text.Json;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using AuthApi.Attributes;
using AuthApi.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace AuthApi.Settings;

public class ConfigureJwtSettings(
    IAmazonKeyManagementService kmsClient,
    ISecretsManagerService secretsManager,
    ILogger<ConfigureJwtSettings> logger)
    : IConfigureOptions<JwtSettings>
{
    public void Configure(JwtSettings options)
    {
        InitializeSettingsAsync(options).GetAwaiter().GetResult();
    }

    private async Task InitializeSettingsAsync(JwtSettings settings)
    {
        // 1. Fetch private key from Secrets Manager if ARN is provided
        if (!string.IsNullOrEmpty(settings.PrivateKeySecret))
        {
            logger.LogInformation("Fetching JWT Private Key from Secrets Manager: {SecretArn}", settings.PrivateKeySecret);
            try
            {
                var secretValue = await secretsManager.GetSecretAsync(settings.PrivateKeySecret);
                var processedSecret = ProcessSecretValue(secretValue);
                
                if (processedSecret.StartsWith("-----BEGIN"))
                {
                    settings.PrivateKey = processedSecret;
                    logger.LogInformation("Successfully retrieved JWT Private Key (PEM format, length: {Length})", settings.PrivateKey.Length);
                }
                else if (processedSecret.StartsWith("AQIC"))
                {
                    settings.PrivateKey = processedSecret;
                    logger.LogInformation("Retrieved secret from Secrets Manager (appears to be KMS ciphertext, length: {Length})", settings.PrivateKey.Length);
                }
                else
                {
                    settings.PrivateKey = processedSecret;
                    logger.LogInformation("Retrieved secret from Secrets Manager (non-PEM format, length: {Length}, start: {Start})", 
                        settings.PrivateKey.Length, 
                        processedSecret.Length > 10 ? processedSecret[..10] : processedSecret);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch JWT Private Key from Secrets Manager");
            }
        }

        // 2. Decrypt properties marked with KmsDecryptionAttribute
        foreach (var prop in typeof(JwtSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<KmsDecryptionAttribute>() != null &&
                prop.GetValue(settings) is string value &&
                !string.IsNullOrEmpty(value))
            {
                // Simple heuristic: if it's already a PEM, skip decryption
                if (value.Contains("-----BEGIN"))
                    continue;

                try
                {
                    var decryptedValue = await DecryptWithKmsAsync(value);
                    prop.SetValue(settings, decryptedValue);
                    logger.LogInformation("Successfully decrypted property {PropertyName} using KMS", prop.Name);
                }
                catch (Exception ex)
                {
                    // If decryption fails, we log it. 
                    // For PrivateKey, this is fatal.
                    if (prop.Name == nameof(JwtSettings.PrivateKey))
                    {
                        var preview = value.Length > 20 ? value[..20] + "..." : value;
                        throw new InvalidOperationException(
                            $"Failed to decrypt {prop.Name} using KMS. The value looks like a ciphertext but decryption failed. " +
                            $"Start: '{preview}', Length: {value.Length}. Error: {ex.Message}", ex);
                    }

                    if (value.Length > 50 && !value.Contains(' '))
                    {
                        logger.LogWarning("KMS decryption failed for {PropertyName}, but the value looks like a ciphertext. Error: {Message}", prop.Name, ex.Message);
                    }
                    else
                    {
                        logger.LogDebug("KMS decryption skipped or failed for {PropertyName}: {Message}", prop.Name, ex.Message);
                    }
                }
            }
        }
    }

    private string ProcessSecretValue(string secretValue)
    {
        if (string.IsNullOrWhiteSpace(secretValue)) return string.Empty;

        var trimmed = secretValue.Trim();

        // If it's a JSON object, try to extract the value
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;
                
                // Common keys for secrets
                string[] commonKeys = ["PrivateKey", "PrivateKeyPem", "Secret", "Value", "Key"];
                foreach (var key in commonKeys)
                {
                    if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        logger.LogInformation("Extracted secret value from JSON key: {Key}", key);
                        return prop.GetString()?.Trim() ?? string.Empty;
                    }
                }

                // If no common key found, just return the first string property
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        logger.LogInformation("Extracted secret value from first JSON key: {Key}", prop.Name);
                        return prop.Value.GetString()?.Trim() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to parse secret value as JSON: {Message}", ex.Message);
            }
        }

        return trimmed;
    }

    private async Task<string> DecryptWithKmsAsync(string encryptedBase64)
    {
        var request = new DecryptRequest
        {
            CiphertextBlob = new MemoryStream(Convert.FromBase64String(encryptedBase64))
        };

        var response = await kmsClient.DecryptAsync(request);
        return Encoding.UTF8.GetString(response.Plaintext.ToArray());
    }
}