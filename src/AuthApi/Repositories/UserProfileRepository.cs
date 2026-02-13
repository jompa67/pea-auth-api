using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;

namespace AuthApi.Repositories
{
    public class UserProfileRepository(IAmazonDynamoDB dynamoDbClient, ILogger<UserProfileRepository> logger)
        : IUserProfileRepository
    {
        private readonly DynamoDBContext _context = new(dynamoDbClient);

        public async Task<bool> CreateUserProfile(UserProfile profile, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveAsync(profile, cancellationToken);
                logger.LogInformation("Created profile for user {UserId} with username {Username}", 
                    profile.UserId, profile.Username);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating profile for user {UserId}", profile.UserId);
                throw;
            }
        }

        public async Task<UserProfile?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.LoadAsync<UserProfile>(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveAsync(profile, cancellationToken);
                logger.LogInformation("Updated profile for user {UserId}", profile.UserId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating profile for user {UserId}", profile.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.DeleteAsync<UserProfile>(userId, cancellationToken);
                logger.LogInformation("Deleted profile for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UserProfile?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(username))
            {
                logger.LogWarning("Attempted to get user profile with null or empty username");
                return null;
            }

            try
            {
                var queryConfig = new DynamoDBOperationConfig
                {
                    IndexName = "UsernameIndex"
                };

                var results = await _context.QueryAsync<UserProfile>(username.ToLowerInvariant(), queryConfig).GetRemainingAsync(cancellationToken);
                return results.SingleOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving profile for username {Username}", username);
                throw;
            }
        }

        public async Task<IEnumerable<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var scanConfig = new List<ScanCondition>()
            {
                // new ScanCondition("EmailVerificationToken", ScanOperator.Equal, "EmailVerificationToken")
            };
            
            var results = await _context.ScanAsync<UserProfile>(scanConfig).GetRemainingAsync(cancellationToken);
            return results;
        }
        
        public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(email))
            {
                logger.LogWarning("Attempted to get user profile with null or empty email");
                return null;
            }

            try
            {
                var queryConfig = new DynamoDBOperationConfig
                {
                    IndexName = "EmailIndex"
                };

                var results = await _context.QueryAsync<UserProfile>(email, queryConfig).GetRemainingAsync(cancellationToken);
                return results.SingleOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving profile for email {Email}", email);
                throw;
            }
        }
    }
}
