using Amazon.DynamoDBv2.DataModel;
using AuthApi.Models.Converters;
using AuthApi.Models.Enums;

namespace AuthApi.Models;

[DynamoDBTable("jm-user-logins")]
public class UserLogin
{
    [DynamoDBHashKey("user_id")]
    [DynamoDBProperty(Converter = typeof(GuidDynamoDbConverter))]
    public required Guid UserId { get; set; }

    [DynamoDBRangeKey("auth_provider")]
    [DynamoDBProperty(Converter = typeof(EnumStringConverter<AuthProvider>))]
    public required AuthProvider AuthProvider { get; set; }

    [DynamoDBProperty(Converter = typeof(EnumStringConverter<AuthType>))]
    public required AuthType AuthType { get; set; }

    public required string AuthValue { get; set; }

    [DynamoDBProperty("created_at", Converter = typeof(Iso8601DynamoDbConverter))]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}