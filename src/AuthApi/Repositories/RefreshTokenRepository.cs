using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;

namespace AuthApi.Repositories
{
    public class RefreshTokenRepository(IAmazonDynamoDB dynamoDbClient, ILogger<RefreshTokenRepository> logger)
        : IRefreshTokenRepository
    {
        private readonly DynamoDBContext _context = new(dynamoDbClient);

        public async Task<RefreshTokenData> GetByTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("Attempted to get refresh token with null or empty value");
                return null;
            }

            try
            {
                var queryConfig = new DynamoDBOperationConfig
                {
                    IndexName = "RefreshTokenIndex"
                };

                var query = _context.QueryAsync<RefreshTokenData>(refreshToken, queryConfig);
                var results = await query.GetRemainingAsync(cancellationToken);
                
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving refresh token: {RefreshToken}", refreshToken);
                throw;
            }
        }

        public async Task<bool> AddAsync(RefreshTokenData tokenData, CancellationToken cancellationToken)
        {
            if (tokenData == null)
            {
                logger.LogWarning("Attempted to add null token data");
                return false;
            }

            try
            {
                if (tokenData.AddedDate == default)
                {
                    tokenData.AddedDate = DateTime.UtcNow;
                }
                
                await _context.SaveAsync(tokenData, cancellationToken);
                logger.LogInformation("Added refresh token for user {UserId}", tokenData.UserId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding refresh token for user {UserId}", tokenData.UserId);
                throw;
            }
        }

        public async Task<IEnumerable<RefreshTokenData>> GetByJwtTokenAsync(string jwtToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                throw new ArgumentException("JWT token cannot be null or empty", nameof(jwtToken));
        
            try
            {
                // Use the high-level DynamoDB operation approach consistent with other methods
                var queryConfig = new DynamoDBOperationConfig
                {
                    IndexName = "TokenIndex"
                };
        
                var query = _context.QueryAsync<RefreshTokenData>(jwtToken, queryConfig);
                    
                var results = await query.GetRemainingAsync(cancellationToken);
                
                logger.LogInformation("Retrieved refresh token for JWT token lookup");
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving refresh token by JWT token");
                throw;
            }
        }
        
        public async Task<bool> UpdateAsync(RefreshTokenData tokenData, CancellationToken cancellationToken)
        {
            if (tokenData == null)
            {
                logger.LogWarning("Attempted to update null token data");
                return false;
            }

            try
            {
                await _context.SaveAsync(tokenData, cancellationToken);
                logger.LogInformation("Updated refresh token: {RefreshToken}", tokenData.RefreshToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating refresh token: {RefreshToken}", tokenData.RefreshToken);
                throw;
            }
        }

        public async Task<List<RefreshTokenData>> GetActiveByjWtTokenAsyncFoo(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Attempted to get active tokens with null or empty user ID");
                return new List<RefreshTokenData>();
            }

            try
            {
                var now = DateTime.UtcNow;
                
                var scanConditions = new List<ScanCondition>
                {
                    new ScanCondition("UserId", ScanOperator.Equal, userId),
                    new ScanCondition("ExpiryDate", ScanOperator.GreaterThan, now),
                    new ScanCondition("IsUsed", ScanOperator.Equal, false),
                    new ScanCondition("IsRevoked", ScanOperator.Equal, false)
                };
                
                var query = _context.ScanAsync<RefreshTokenData>(scanConditions);
                var results = await query.GetRemainingAsync(cancellationToken);
                
                logger.LogInformation("Retrieved {Count} active tokens for user {UserId}", results.Count, userId);
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving active tokens for user {UserId}", userId);
                throw;
            }
        }
        
        public async Task<List<RefreshTokenData>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Attempted to get active tokens with null or empty user ID");
                return new List<RefreshTokenData>();
            }

            try
            {
                var now = DateTime.UtcNow;
                
                var scanConditions = new List<ScanCondition>
                {
                    new ScanCondition("UserId", ScanOperator.Equal, userId),
                    new ScanCondition("ExpiryDate", ScanOperator.GreaterThan, now),
                    new ScanCondition("IsUsed", ScanOperator.Equal, false),
                    new ScanCondition("IsRevoked", ScanOperator.Equal, false)
                };
                
                var query = _context.ScanAsync<RefreshTokenData>(scanConditions);
                var results = await query.GetRemainingAsync(cancellationToken);
                
                logger.LogInformation("Retrieved {Count} active tokens for user {UserId}", results.Count, userId);
                return results;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving active tokens for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> RevokeAllForJwtTokenAsync(string jwtToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                logger.LogWarning("Attempted to revoke tokens with null or empty user ID");
                return false;
            }

            try
            {
                var activeTokens = await GetByJwtTokenAsync(jwtToken, cancellationToken);
                
                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    await UpdateAsync(token, cancellationToken);
                }
                
                logger.LogInformation("Revoked {Count} tokens for user {JwtToken}", activeTokens.Count(), jwtToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error revoking tokens for user {JwtToken}", jwtToken);
                throw;
            }
        }
    }
}