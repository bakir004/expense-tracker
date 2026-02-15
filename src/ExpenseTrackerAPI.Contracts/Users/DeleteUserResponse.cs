namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Response contract for user deletion operations.
/// </summary>
/// <param name="Id">Unique identifier of the deleted user</param>
/// <param name="Name">Name of the deleted user</param>
/// <param name="Email">Email of the deleted user</param>
/// <param name="Message">Confirmation message indicating successful deletion</param>
public record DeleteUserResponse(
    int Id,
    string Name,
    string Email,
    string Message);
