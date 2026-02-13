using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AuthApi.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddConfig<JwtSettings>(configuration)
            .ConfigureOptions<ConfigureJwtSettings>()
            .ConfigureOptions<ConfigureJwtBearerOptions>()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        return services;
    }

    public static IServiceCollection AddDynamoDb(this IServiceCollection services)
    {
        services
            .AddAWSService<IAmazonDynamoDB>()
            .AddSingleton<IDynamoDBContext>(sp =>
            {
                var dynamoDbClient = sp.GetRequiredService<IAmazonDynamoDB>();
                return new DynamoDBContext(dynamoDbClient);
            });

        return services;
    }
}