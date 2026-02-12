namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for successful user authentication.
/// </summary>
public record LoginResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    string Token,
    DateTime ExpiresAt);
