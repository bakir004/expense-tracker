namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for user profile update operations.
/// </summary>
/// <param name="Id">Unique identifier for the user</param>
/// <param name="Name">User's updated full name</param>
/// <param name="Email">User's updated email address</param>
/// <param name="InitialBalance">User's updated account balance</param>
/// <param name="UpdatedAt">UTC timestamp when the user profile was last updated</param>
public record UpdateUserResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime UpdatedAt);
