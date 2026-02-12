using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for updating user profile information.
/// </summary>
public record UpdateUserRequest(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    string Email,

    [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    string? NewPassword,

    [Required(ErrorMessage = "Current password is required for verification")]
    [StringLength(100, ErrorMessage = "Current password cannot exceed 100 characters")]
    string CurrentPassword,

    decimal InitialBalance);
