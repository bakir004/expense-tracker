namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for successful user registration.
/// </summary>
public record RegisterResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime CreatedAt);
