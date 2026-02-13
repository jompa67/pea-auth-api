using System.Security.Claims;

namespace AuthApi.Services.Interface;

public interface IJwtTokenGenerator
{
    TokenResult GenerateToken(IEnumerable<Claim> claims);
}