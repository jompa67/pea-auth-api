using Amazon.DynamoDBv2.DataModel;

namespace AuthApi.Models;

[DynamoDBTable("jm-refresh-token")]
public class RefreshTokenDataxxx
{
    [DynamoDBHashKey]
    public string RefreshToken { get; set; } // The actual refresh token string
    
    [DynamoDBProperty]
    public string Token { get; set; } // The actual refresh token string
    
    [DynamoDBProperty]
    public DateTime ExpiryDate { get; set; }
    [DynamoDBProperty]
    public bool IsUsed { get; set; }
    [DynamoDBProperty]
    public bool IsRevoked { get; set; }
    [DynamoDBProperty]
    public DateTime AddedDate { get; set; }
    [DynamoDBProperty]
    public string UserId { get; set; } 
}