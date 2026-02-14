using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace AuthApi.Services;

public interface ISecretsManagerService
{
    Task<string> GetSecretAsync(string secretArn);
}

public class SecretsManagerService : ISecretsManagerService
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly ILogger<SecretsManagerService> _logger;

    public SecretsManagerService(IAmazonSecretsManager secretsManager, ILogger<SecretsManagerService> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretArn)
    {
        try
        {
            _logger.LogDebug("Fetching secret from ARN: {SecretArn}", secretArn);

            var request = new GetSecretValueRequest
            {
                SecretId = secretArn
            };

            var response = await _secretsManager.GetSecretValueAsync(request);

            _logger.LogDebug("Successfully retrieved secret from Secrets Manager");

            return response.SecretString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret from Secrets Manager: {SecretArn}", secretArn);
            throw;
        }
    }
}
