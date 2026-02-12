namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for user deletion operations.
/// </summary>
public record DeleteUserResponse(
    int Id,
    string Name,
    string Email,
    string Message);
