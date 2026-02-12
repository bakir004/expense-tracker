using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

/// <summary>
/// Domain-specific errors for user operations.
/// </summary>
public static class UserErrors
{
    public static Error NotFound =>
        Error.NotFound("user", "User not found.");

    public static Error DuplicateEmail =>
        Error.Conflict("email", "A user with this email already exists.");

    public static Error InvalidEmail =>
        Error.Validation("email", "The provided email is invalid.");

    public static Error InvalidName =>
        Error.Validation("name", "Name must be between 1 and 100 characters.");

    public static Error InvalidPassword =>
        Error.Validation("password", "Password is required and must be at least 6 characters long.");
}
