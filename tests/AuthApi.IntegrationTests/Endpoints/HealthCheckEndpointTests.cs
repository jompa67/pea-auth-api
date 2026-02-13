using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AuthApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace AuthApi.IntegrationTests.Endpoints
{
    public class HealthCheckEndpointTests
    {
        private WebApplicationFactory<Program> factory;
        private HttpClient client;

        public HealthCheckEndpointTests()
        {
            factory = new WebApplicationFactory<Program>();
                // .WithWebHostBuilder(builder =>
                // {
                //     // Configure your test environment here if needed
                //     // builder.ConfigureAppConfiguration((hostingContext, config) =>
                //     // {
                //     //     config.AddInMemoryCollection(new Dictionary<string, string>
                //     //     {
                //     //         ["SomeSetting"] = "testValue"
                //     //     });
                //     // });
                // });
            client = factory.CreateClient();
        }

        // [OneTimeTearDown]
        // public void OneTimeTearDown()
        // {
        //     client.Dispose();
        //     factory.Dispose();
        // }

        // protected override IHostBuilder CreateHostBuilder()
        // {
        //     return base.CreateHostBuilder()
        //         .ConfigureWebHostDefaults(webBuilder =>
        //         {
        //             webBuilder.UseContentRoot("."); // Or specify the actual path if needed
        //             webBuilder.UseStartup<Program>();
        //         });
        // }
        
        // [Fact]
        public async Task Helthcheck_Endpoint_ReturnsOkWithCorrectData()
        {
            // Act
            var response = await client.GetAsync("/auth/helthcheck");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read response content
            var content = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<HealthCheckResponse>(content);
            
            // Verify response structure
            responseData.Message.Should().Be("AuthApi is up and running");
            responseData.Time.Should().NotBeNullOrEmpty();
            
            // Verify time format
            DateTime.TryParseExact(
                responseData.Time, 
                "yyyy-MM-dd HH:mm:ss", 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, 
                out var parsedTime).Should().BeTrue($"Time '{responseData.Time}' should be in format 'yyyy-MM-dd HH:mm:ss'");
            
            // Time should be reasonably close to now (within 10 seconds)
            parsedTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }
        
        // Response model for deserialization
        private class HealthCheckResponse
        {
            [JsonProperty("message")]
            public string Message { get; set; }
            
            [JsonProperty("time")]
            public string Time { get; set; }
        }
    }
}
