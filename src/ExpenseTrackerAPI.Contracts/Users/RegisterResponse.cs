namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for successful user registration.
/// </summary>
/// <param name="Id">Unique identifier for the newly created user</param>
/// <param name="Name">User's full name</param>
/// <param name="Email">User's email address</param>
/// <param name="InitialBalance">Starting account balance</param>
/// <param name="CreatedAt">UTC timestamp when the user account was created</param>
public record RegisterResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime CreatedAt);
