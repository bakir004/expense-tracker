namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for successful user authentication.
/// </summary>
/// <param name="Id">Unique identifier for the authenticated user</param>
/// <param name="Name">User's full name</param>
/// <param name="Email">User's email address</param>
/// <param name="InitialBalance">User's account balance</param>
/// <param name="Token">JWT access token to be used for authenticating subsequent API requests</param>
/// <param name="ExpiresAt">UTC timestamp when the token expires (typically 24 hours from login)</param>
public record LoginResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    string Token,
    DateTime ExpiresAt);
