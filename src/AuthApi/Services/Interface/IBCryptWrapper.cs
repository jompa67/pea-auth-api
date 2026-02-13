namespace AuthApi.Services.Interface;

/// <summary>
/// Interface for wrapping BCrypt.Net functionality to make it testable
/// </summary>
public interface IBCryptWrapper
{
    /// <summary>
    /// Verifies if a password matches the hashed value
    /// </summary>
    /// <param name="password">The plaintext password</param>
    /// <param name="hashedPassword">The hashed password</param>
    /// <returns>True if the password matches, false otherwise</returns>
    bool Verify(string password, string hashedPassword);
    
    /// <summary>
    /// Hashes a password
    /// </summary>
    /// <param name="password">The plaintext password to hash</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);
}
