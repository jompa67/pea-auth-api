using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthApi.Services.Interface;
using AuthApi.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Services;

public class TokenResult
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
    public DateTime IssuedAt { get; set; }
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly RsaSecurityKey _rsaSecurityKey;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;

        if (string.IsNullOrEmpty(_jwtSettings.PrivateKey))
            throw new InvalidOperationException("RSA Private Key is missing.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_jwtSettings.PrivateKey);
        _rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true));
    }

    /// <summary>
    /// Generates a JWT token with the specified claims
    /// </summary>
    /// <param name="claims">The claims to include in the token</param>
    /// <returns>Token result with the generated token and metadata</returns>
    public TokenResult GenerateToken(IEnumerable<Claim> claims)
    {
        if (claims == null)
            throw new ArgumentNullException(nameof(claims));

        if (!claims.Any())
            throw new ArgumentException(nameof(claims));
        
        var issuedAt = DateTime.UtcNow;
        var expiration = issuedAt.AddMinutes(_jwtSettings.ExpirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            IssuedAt = issuedAt,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new TokenResult
        {
            Token = tokenHandler.WriteToken(token),
            Expiration = expiration,
            IssuedAt = issuedAt
        };
    }
    
    /// <summary>
    /// Creates claims for a user with the specified roles
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="username">The username</param>
    /// <param name="roles">The roles to assign to the user</param>
    /// <returns>A collection of claims</returns>
    public static IEnumerable<Claim> CreateUserClaims(string userId, string username, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
        };
        
        // Add each role as a separate claim
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        return claims;
    }
    
    /// <summary>
    /// Adds admin role to the specified claims collection
    /// </summary>
    /// <param name="claims">The existing claims collection</param>
    /// <returns>Updated claims collection with admin role</returns>
    public static IEnumerable<Claim> AddAdminRole(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        claimsList.Add(new Claim(ClaimTypes.Role, "Admin"));
        return claimsList;
    }
    
    /// <summary>
    /// Adds user role to the specified claims collection
    /// </summary>
    /// <param name="claims">The existing claims collection</param>
    /// <returns>Updated claims collection with user role</returns>
    public static IEnumerable<Claim> AddUserRole(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        claimsList.Add(new Claim(ClaimTypes.Role, "User"));
        return claimsList;
    }
    
    /// <summary>
    /// Creates an RSA public key from PEM format string
    /// </summary>
    /// <param name="publicKeyPem">The public key in PEM format</param>
    /// <returns>RSA parameters for the public key</returns>
    public static RSAParameters CreateRsaPublicKey(string publicKeyPem)
    {
        if (string.IsNullOrEmpty(publicKeyPem))
            throw new ArgumentNullException(nameof(publicKeyPem), "Public key cannot be null or empty");
            
        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(publicKeyPem);
            return rsa.ExportParameters(false);
        }
        catch (Exception ex)
        {
            throw new FormatException("Invalid public key format. The key must be in PEM format.", ex);
        }
    }
}