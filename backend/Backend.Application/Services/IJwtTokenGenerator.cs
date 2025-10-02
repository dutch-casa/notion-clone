namespace Backend.Application.Services;

/// <summary>
/// Service for generating JWT tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate a JWT token for a user.
    /// </summary>
    string GenerateToken(Guid userId, string email, string name);
}
