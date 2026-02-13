using Amazon.Lambda.AspNetCoreServer;

namespace AuthApi;

public class LambdaEntryPoint : APIGatewayProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseStartup<Startup>()
            .UseLambdaServer();
    }
}