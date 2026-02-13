namespace AuthApi.Contracts.Register;

public class RegisterWithPasswordRequest
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    
    public bool IsTestAccount { get; set; }
}