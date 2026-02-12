using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for user registration.
/// </summary>
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
