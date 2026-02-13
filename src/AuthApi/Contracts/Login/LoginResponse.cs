namespace AuthApi.Contracts.Login;

public class LoginResponse
{
    public string? Token { get; set; }
    public DateTime Expiration { get; set; }
    public bool IsSuccess { get; set; }
    public required string ErrorMessage { get; set; }
    public string? RefreshToken { get; set; }
}