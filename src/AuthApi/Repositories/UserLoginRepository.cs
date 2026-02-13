using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AuthApi.Models;
using AuthApi.Models.Enums;
using AuthApi.Repositories.Interfaces;

namespace AuthApi.Repositories
{
    public class UserLoginRepository : IUserLoginRepository
    {
        private readonly DynamoDBContext _context;
        private readonly ILogger<UserLoginRepository> _logger;

        public UserLoginRepository(IAmazonDynamoDB dynamoDbClient, ILogger<UserLoginRepository> logger)
        {
            _context = new DynamoDBContext(dynamoDbClient);
            _logger = logger;
        }

        public async Task<bool> AddAsync(UserLogin login)
        {
            return await CreateUserLogin(login);
        }

        public async Task<bool> CreateUserLogin(UserLogin login)
        {
            try
            {
                var existingLogin = await GetByUserIdAndProviderAsync(login.UserId, login.AuthProvider);
                if (existingLogin != null)
                {
                    _logger.LogWarning("User {UserId} already has a login method for {AuthProvider}", 
                        login.UserId, login.AuthProvider);
                    throw new InvalidOperationException("User already has this login method.");
                }

                await _context.SaveAsync(login);
                _logger.LogInformation("Created login for user {UserId} with provider {AuthProvider}", 
                    login.UserId, login.AuthProvider);
                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error creating login for user {UserId} with provider {AuthProvider}", 
                    login.UserId, login.AuthProvider);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(UserLogin login)
        {
            try
            {
                await _context.SaveAsync(login);
                _logger.LogInformation("Updated login for user {UserId} with provider {AuthProvider}", 
                    login.UserId, login.AuthProvider);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating login for user {UserId} with provider {AuthProvider}", 
                    login.UserId, login.AuthProvider);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid userId, AuthProvider authProvider)
        {
            try
            {
                await _context.DeleteAsync<UserLogin>(userId, authProvider);
                _logger.LogInformation("Deleted login for user {UserId} with provider {AuthProvider}", 
                    userId, authProvider);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting login for user {UserId} with provider {AuthProvider}", 
                    userId, authProvider);
                throw;
            }
        }

        public async Task<UserLogin> GetByUserIdAndProviderAsync(Guid userId, AuthProvider authProvider)
        {
            try
            {
                return await _context.LoadAsync<UserLogin>(userId, authProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login for user {UserId} with provider {AuthProvider}", 
                    userId, authProvider);
                throw;
            }
        }

        public async Task<List<UserLogin>> GetAllByUserIdAsync(Guid userId)
        {
            try
            {
                var query = _context.QueryAsync<UserLogin>(userId);
                return await query.GetNextSetAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all logins for user {UserId}", userId);
                throw;
            }
        }
    }
}
