using System;
using Amazon.DynamoDBv2.DataModel;

namespace AuthApi.Models;

[DynamoDBTable("jm-refresh-token")]
public class RefreshTokenData
{
    [DynamoDBHashKey]
    public required string RefreshToken { get; set; }
    
    [DynamoDBGlobalSecondaryIndexHashKey("TokenIndex")]
    public required string Token { get; set; }
    
    public required string UserId { get; set; }
    
    public DateTime ExpiryDate { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public bool IsRevoked { get; set; } = false;
    
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    
    public string RevokedReason { get; set; } = null;
    
    public DateTime? RevokedDate { get; set; } = null;
    
    public string ReplacedByToken { get; set; } = null;
    
    [DynamoDBIgnore]
    public bool IsActive => !IsUsed && !IsRevoked && ExpiryDate > DateTime.UtcNow;
}
