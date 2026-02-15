using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for updating user profile information. Only provide fields you want to update.
/// </summary>
/// <param name="Name">Optional - User's full name (max 100 characters, only updated if provided)</param>
/// <param name="Email">Optional - User's email address (must be in valid format and max 254 characters, only updated if provided)</param>
/// <param name="NewPassword">Optional - New password (must be 8-100 characters with at least one uppercase, one lowercase, one digit, and one special character)</param>
/// <param name="CurrentPassword">Required - User's current password for verification (always required for security)</param>
/// <param name="InitialBalance">Optional - Account balance (only updated if provided)</param>
public record UpdateUserRequest(
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string? Name,

    [EmailAddress(ErrorMessage = "Email format is invalid")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    string? Email,

    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    string? NewPassword,

    [Required(ErrorMessage = "Current password is required for verification")]
    [StringLength(100, ErrorMessage = "Current password cannot exceed 100 characters")]
    string CurrentPassword,

    decimal? InitialBalance);
