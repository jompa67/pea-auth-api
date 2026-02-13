using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthApi.Services;
using AuthApi.Services.Interface;
using AuthApi.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Tests.Services
{
    public class JwtTokenGeneratorTests
    {
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly JwtSettings _jwtSettings;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly RSA _rsaKey;

        public JwtTokenGeneratorTests()
        {
            // Setup RSA key for testing
            _rsaKey = RSA.Create();
            
            // Setup JWT settings with test values
            _jwtSettings = new JwtSettings
            {
                PrivateKey = _rsaKey.ExportRSAPrivateKeyPem(),
                PublicKey = _rsaKey.ExportRSAPublicKeyPem(),
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpirationMinutes = 60,
                ClockSkewMinutes = 5
            };

            // Mock the options using NSubstitute
            _jwtOptions = Substitute.For<IOptions<JwtSettings>>();
            _jwtOptions.Value.Returns(_jwtSettings);

            // Create the service instance to test
            _jwtTokenGenerator = new JwtTokenGenerator(_jwtOptions);
        }

        [Fact]
        public void GenerateToken_WithValidClaims_ReturnsTokenResult()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNull().And.NotBeEmpty();
            result.Expiration.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void GenerateToken_TokenContainsExpectedClaims()
        {
            // Arrange
            var username = "testuser";
            var email = "test@example.com";
            var role = "User";
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(result.Token);

            jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value.Should().Be(username);
            jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value.Should().Be(email);
            jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value.Should().Be(role);
        }

        [Fact]
        public void GenerateToken_TokenHasCorrectIssuerAndAudience()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser")
            };

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(result.Token);

            jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
            jwtToken.Audiences.FirstOrDefault().Should().Be(_jwtSettings.Audience);
        }

        [Fact]
        public void GenerateToken_TokenHasCorrectExpirationTime()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser")
            };
            var now = DateTime.UtcNow;

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            // Allow for a small timing difference in execution (5 seconds)
            var expectedExpiration = now.AddMinutes(_jwtSettings.ExpirationMinutes);
            var difference = Math.Abs((result.Expiration - expectedExpiration).TotalSeconds);
            difference.Should().BeLessThan(5, $"Expiration difference ({difference} seconds) exceeds threshold");
        }

        [Fact]
        public void GenerateToken_TokenCanBeValidated()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "User")
            };

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Create validation parameters using the public key
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new RsaSecurityKey(_rsaKey),
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
            };

            // This will throw if validation fails
            var principal = tokenHandler.ValidateToken(result.Token, validationParameters, out var validatedToken);
            
            principal.Should().NotBeNull();
            validatedToken.Should().NotBeNull();
            principal.Identity?.Name.Should().Be("testuser");
            principal.IsInRole("User").Should().BeTrue();
        }

        [Fact]
        public void GenerateToken_WithMultipleRoles_ContainsAllRoleClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(result.Token);

            var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").ToList();
            roleClaims.Should().HaveCount(2);
            roleClaims.Should().Contain(c => c.Value == "User");
            roleClaims.Should().Contain(c => c.Value == "Admin");
        }

        [Fact]
        public void GenerateToken_WithCustomClaims_ContainsCustomClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim("CustomClaim", "CustomValue"),
                new Claim("UserId", "12345")
            };

            // Act
            var result = _jwtTokenGenerator.GenerateToken(claims);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(result.Token);

            jwtToken.Claims.FirstOrDefault(c => c.Type == "CustomClaim")?.Value.Should().Be("CustomValue");
            jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value.Should().Be("12345");
        }

        [Fact]
        public void GenerateToken_WithNullClaims_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<Claim> claims = null;

            // Act & Assert
            FluentActions.Invoking(() => _jwtTokenGenerator.GenerateToken(claims))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GenerateToken_WithEmptyClaims_ThrowsArgumentException()
        {
            // Arrange
            var claims = new List<Claim>();

            // Act & Assert
            FluentActions.Invoking(() => _jwtTokenGenerator.GenerateToken(claims))
                .Should().Throw<ArgumentException>()
                .Which.Message.Should().Contain("claims");
        }
    }
}
