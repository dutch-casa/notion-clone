namespace Backend.Application.Services;

/// <summary>
/// Service for hashing and verifying passwords.
/// Implementation should use a secure algorithm like BCrypt or Argon2.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
