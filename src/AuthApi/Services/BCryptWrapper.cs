using AuthApi.Services.Interface;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AuthApi.Services;

/// <summary>
/// Implementation of IBCryptWrapper that uses BCrypt.Net
/// </summary>
public class BCryptWrapper : IBCryptWrapper
{
    /// <inheritdoc />
    public bool Verify(string password, string hashedPassword)
    {
        return BCryptNet.Verify(password, hashedPassword);
    }
    
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password);
    }
}
