using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for user login/authentication.
/// </summary>
/// <param name="Email">User's registered email address (required, must be in valid email format)</param>
/// <param name="Password">User's password (required)</param>
public record LoginRequest(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Password is required")]
    string Password);
