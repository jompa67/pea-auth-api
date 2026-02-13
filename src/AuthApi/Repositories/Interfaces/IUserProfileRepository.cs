using AuthApi.Models;

namespace AuthApi.Repositories.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
        Task<IEnumerable<UserProfile>> GetAllAsync(CancellationToken cancellationToken);
        Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<bool> CreateUserProfile(UserProfile profile, CancellationToken cancellationToken);
        Task<UserProfile?> GetByIdAsync(string userId, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(UserProfile profile, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken);
    }
}
