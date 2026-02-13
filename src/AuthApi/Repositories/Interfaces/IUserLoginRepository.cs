using AuthApi.Models;
using AuthApi.Models.Enums;

namespace AuthApi.Repositories.Interfaces
{
    public interface IUserLoginRepository
    {
        Task<bool> CreateUserLogin(UserLogin login);
        Task<UserLogin> GetByUserIdAndProviderAsync(Guid userId, AuthProvider authProvider);
        Task<List<UserLogin>> GetAllByUserIdAsync(Guid userId);
        Task<bool> UpdateAsync(UserLogin login);
        Task<bool> DeleteAsync(Guid userId, AuthProvider authProvider);
    }
}
