using System.Reflection;
using System.Text;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using AuthApi.Attributes;
using Microsoft.Extensions.Options;

namespace AuthApi.Settings;

public class ConfigureJwtSettings(IAmazonKeyManagementService kmsClient)
    : IConfigureOptions<JwtSettings>
{
    public void Configure(JwtSettings options)
    {
        DecryptKmsPropertiesAsync(options).GetAwaiter().GetResult();
    }

    private async Task DecryptKmsPropertiesAsync(JwtSettings settings)
    {
        foreach (var prop in typeof(JwtSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (prop.GetCustomAttribute<KmsDecryptionAttribute>() != null &&
                prop.GetValue(settings) is string encryptedValue)
            {
                var decryptedValue = await DecryptWithKmsAsync(encryptedValue);
                prop.SetValue(settings, decryptedValue);
            }
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