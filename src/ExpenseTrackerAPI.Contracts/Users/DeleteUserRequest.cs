using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for user deletion operations.
/// </summary>
/// <param name="CurrentPassword">User's current password for verification (required for security)</param>
/// <param name="ConfirmDeletion">Explicit confirmation flag - must be set to true to proceed with account deletion (required)</param>
public record DeleteUserRequest(
    [Required(ErrorMessage = "Current password is required for account deletion")]
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    string CurrentPassword,

    [Required(ErrorMessage = "Confirmation is required")]
    bool ConfirmDeletion = false);
