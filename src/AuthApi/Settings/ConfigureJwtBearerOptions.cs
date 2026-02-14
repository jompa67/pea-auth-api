using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Settings;

public class ConfigureJwtBearerOptions(IOptions<JwtSettings> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public void Configure(JwtBearerOptions options)
    {
        try
        {
            var rsaPublic = RSA.Create();
            rsaPublic.ImportFromPem(_jwtSettings.PublicKey);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes),
                IssuerSigningKey = new RsaSecurityKey(rsaPublic),
                
                // Add proper claim type mapping for role-based authorization
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
        }
        catch (ArgumentException ex) when (ex.Message.Contains("No supported key formats were found"))
        {
            var keyPreview = _jwtSettings.PublicKey?.Length > 20 
                ? _jwtSettings.PublicKey[..20] + "..." 
                : _jwtSettings.PublicKey;
            
            var isKmsCiphertext = _jwtSettings.PublicKey?.StartsWith("AQIC") ?? false;
            var additionalHint = isKmsCiphertext 
                ? " This looks like an encrypted KMS ciphertext. Note that the public key should usually be a plain PEM string, or marked with [KmsDecryption] if it is encrypted."
                : "";

            throw new InvalidOperationException($"Invalid RSA Public Key format. The key must be a PEM-encoded string.{additionalHint} Start of key: '{keyPreview}', Length: {_jwtSettings.PublicKey?.Length ?? 0}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize JWT Public Key. Ensure JwtSettings__PublicKey is a valid PEM string. Length: {_jwtSettings.PublicKey?.Length ?? 0}", ex);
        }
        
        // Optionally, you can add events for additional JWT processing
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // Access the validated principal
                var principal = context.Principal;
                
                // You can add additional validation or logging here if needed
                // Example: Check if principal has required roles
                
                return Task.CompletedTask;
            }
        };
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        Configure(options);
    }
}