using Amazon.DynamoDBv2.DataModel;
using AuthApi.Models.Converters;

namespace AuthApi.Models;

[DynamoDBTable("jm-users")]
public class UserProfile
{
    [DynamoDBHashKey("id")]
    [DynamoDBProperty(Converter = typeof(GuidDynamoDbConverter))]
    public required Guid UserId { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("UsernameIndex", AttributeName = "username")]
    public required string Username { get; set; }

    [DynamoDBGlobalSecondaryIndexHashKey("EmailIndex", AttributeName = "email")]
    public required string Email { get; set; }

    public required string UsernameOriginal { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsUserRole { get; set; } = true;
    public bool IsAdminRole { get; set; }

    [DynamoDBProperty("created_at", Converter = typeof(Iso8601DynamoDbConverter))]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime EmailVerifiedAt { get; set; }
    public bool IsTestAccount { get; set; }
}