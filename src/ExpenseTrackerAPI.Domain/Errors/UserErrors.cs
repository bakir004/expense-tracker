using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

/// <summary>
/// Domain-specific errors for user operations.
/// </summary>
public static class UserErrors
{
    public static Error NotFound =>
        Error.NotFound("User", "User not found.");

    public static Error DuplicateEmail =>
        Error.Conflict("Email", "A user with this email already exists.");

    public static Error InvalidEmail =>
        Error.Validation("Email", "Email format is invalid.");

    public static Error InvalidName =>
        Error.Validation("Name", "Name is required and cannot exceed 100 characters.");

    public static Error InvalidPassword =>
        Error.Validation("Password", "Password is required and must be at least 8 characters long.");

    public static Error WeakPassword =>
        Error.Validation("Password", "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

    public static Error EmailRequired =>
        Error.Validation("Email", "Email is required.");

    public static Error NameRequired =>
        Error.Validation("Name", "Name is required.");

    public static Error PasswordRequired =>
        Error.Validation("Password", "Password is required.");

    public static Error InvalidCredentials =>
        Error.Unauthorized("Credentials", "Invalid email or password.");

    public static Error InvalidUserId =>
        Error.Validation("UserId", "User ID must be a positive integer.");

    public static Error ConcurrencyError =>
        Error.Conflict("Concurrency", "User was modified by another process.");

    public static Error DeletionConfirmation =>
        Error.Validation("Confirmation", "Deletion must be confirmed to proceed.");

    public static Error RegistrationFailed(string message) =>
        Error.Failure("Registration", $"Registration failed: {message}");

    public static Error LoginFailed(string message) =>
        Error.Failure("Login", $"Login failed: {message}");

    public static Error UpdateFailed(string message) =>
        Error.Failure("Update", $"Update failed: {message}");

    public static Error DeleteFailed(string message) =>
        Error.Failure("Delete", $"Delete operation failed: {message}");
}
