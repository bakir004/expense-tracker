// ============================================================================
// FILE: UserValidator.cs
// ============================================================================
// WHAT: Static validation class for user-related input validation.
//
// WHY: This validator exists in the Application layer to centralize validation
//      logic for user operations. Validation is an application concern because
//      it enforces business rules about what constitutes valid user data.
//      Separating validation into its own class keeps the service clean and
//      makes validation logic reusable and testable independently.
//
// WHAT IT DOES:
//      - Validates user creation requests (name, email, password)
//      - Checks name length (1-100 characters)
//      - Validates email format using MailAddress
//      - Ensures password meets minimum length requirement (6 characters)
//      - Returns ErrorOr<Success> with appropriate domain errors if validation fails
//      - Used by UserService before processing user creation requests
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Errors;

namespace SampleCkWebApp.Application.Users;

/// <summary>
/// Validator for user-related operations.
/// Contains validation logic for user creation and updates.
/// </summary>
public static class UserValidator
{
    /// <summary>
    /// Validates user creation request parameters
    /// </summary>
    public static ErrorOr<Success> ValidateCreateUserRequest(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return UserErrors.InvalidName;
        }

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
        {
            return UserErrors.InvalidEmail;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return UserErrors.InvalidPassword;
        }

        return Result.Success;
    }
    
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

