using Microsoft.Extensions.Configuration;
using Refit;
using System;
using System.Net.Http;

namespace AuthApi.SystemTests.Services
{
    /// <summary>
    /// Service for accessing the Hello API endpoint
    /// </summary>
    public class HelloApiService
    {
        private readonly IAuthApi authApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelloApiService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration containing API settings</param>
        public HelloApiService(IConfiguration configuration)
        {
            var apiAuthBaseUrl = Environment.GetEnvironmentVariable("ApiAuthBaseUrl");
            var baseUrl = apiAuthBaseUrl ?? "https://localhost:7299/auth";
            
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };

            authApi = RestService.For<IAuthApi>(httpClient);
        }

        public HelloApiService()
        {
        }

        /// <summary>
        /// Gets a hello message from the API
        /// </summary>
        /// <returns>The hello message response</returns>
        public async System.Threading.Tasks.Task<string> GetHelloMessageAsync()
        {
            return await authApi.HealthCheck();
        }
    }
}
