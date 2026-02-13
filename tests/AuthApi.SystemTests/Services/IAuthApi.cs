using Refit;
using System.Threading.Tasks;

namespace AuthApi.SystemTests.Services
{
    /// <summary>
    /// Interface for accessing the Hello API endpoint
    /// </summary>
    public interface IAuthApi
    {
        [Get("/api/auth/healthcheck")]
        Task<string> HealthCheck();
    }
}
