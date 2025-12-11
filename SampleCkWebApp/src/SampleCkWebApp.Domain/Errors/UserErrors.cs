using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class UserErrors
{
    public static Error NotFound =>
        Error.NotFound($"{nameof(UserErrors)}.{nameof(NotFound)}", "User not found.");
    
    public static Error DuplicateEmail =>
        Error.Conflict($"{nameof(UserErrors)}.{nameof(DuplicateEmail)}", "A user with this email already exists.");
    
    public static Error InvalidEmail =>
        Error.Validation($"{nameof(UserErrors)}.{nameof(InvalidEmail)}", "The provided email is invalid.");
    
    public static Error InvalidName =>
        Error.Validation($"{nameof(UserErrors)}.{nameof(InvalidName)}", "Name must be between 1 and 100 characters.");
    
    public static Error InvalidPassword =>
        Error.Validation($"{nameof(UserErrors)}.{nameof(InvalidPassword)}", "Password is required and must be at least 6 characters long.");
}

