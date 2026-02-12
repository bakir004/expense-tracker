namespace ExpenseTrackerAPI.Application.Common.Interfaces;

/// <summary>
/// Interface for JWT token generation and validation.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generate a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(int userId, string email, string name);

    /// <summary>
    /// Get the token expiration time in hours.
    /// </summary>
    int TokenExpirationHours { get; }
}
