using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for user registration.
/// </summary>
/// <param name="Name">User's full name (required, max 100 characters)</param>
/// <param name="Email">User's email address - must be unique and in valid format (required, max 254 characters)</param>
/// <param name="Password">User's password - must be 8-100 characters with at least one uppercase, one lowercase, one digit, and one special character (required)</param>
/// <param name="InitialBalance">Optional starting account balance (defaults to 0.00 if not provided)</param>
public record RegisterRequest(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email format is invalid")]
    [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    string Password,

    decimal? InitialBalance = null);
