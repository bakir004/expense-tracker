namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for user profile update operations.
/// </summary>
public record UpdateUserResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime UpdatedAt);
