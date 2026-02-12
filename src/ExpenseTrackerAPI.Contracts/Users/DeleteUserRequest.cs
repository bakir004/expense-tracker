using System.ComponentModel.DataAnnotations;

namespace ExpenseTrackerAPI.Contracts.Users;

/// <summary>
/// Request contract for user deletion operations.
/// </summary>
public record DeleteUserRequest(
    [Required(ErrorMessage = "Current password is required for account deletion")]
    [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    string CurrentPassword,

    [Required(ErrorMessage = "Confirmation is required")]
    bool ConfirmDeletion = false);
