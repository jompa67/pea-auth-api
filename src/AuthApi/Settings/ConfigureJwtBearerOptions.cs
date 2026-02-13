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