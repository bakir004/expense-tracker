using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

public static class UserErrors
{
    public static Error NotFound =>
        Error.NotFound("User.NotFound", "User not found.");

    public static Error DuplicateEmail =>
        Error.Conflict("User.DuplicateEmail", "A user with this email already exists.");

    public static Error InvalidEmail =>
        Error.Validation("User.InvalidEmail", "Email format is invalid.");

    public static Error InvalidName =>
        Error.Validation("User.InvalidName", "Name is required and cannot exceed 100 characters.");

    public static Error InvalidPassword =>
        Error.Validation("User.InvalidPassword", "Password is required and must be at least 8 characters long.");

    public static Error WeakPassword =>
        Error.Validation("User.WeakPassword", "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

    public static Error EmailRequired =>
        Error.Validation("User.EmailRequired", "Email is required.");

    public static Error NameRequired =>
        Error.Validation("User.NameRequired", "Name is required.");

    public static Error PasswordRequired =>
        Error.Validation("User.PasswordRequired", "Password is required.");

    public static Error InvalidCredentials =>
        Error.Unauthorized("User.InvalidCredentials", "Invalid email or password.");

    public static Error InvalidUserId =>
        Error.Validation("User.InvalidUserId", "User ID must be a positive integer.");

    public static Error ConcurrencyError =>
        Error.Validation("User.ConcurrencyError", "User was modified by another process.");
}
