using AuthApi.Models;

namespace AuthApi.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshTokenData> GetByTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<IEnumerable<RefreshTokenData>> GetByJwtTokenAsync(string jwtToken, CancellationToken cancellationToken);
        Task<bool> AddAsync(RefreshTokenData tokenData, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(RefreshTokenData tokenData, CancellationToken cancellationToken);
        Task<List<RefreshTokenData>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken);
        Task<bool> RevokeAllForJwtTokenAsync(string userId, CancellationToken cancellationToken);
    }
}
