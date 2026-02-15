using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Users;
using Microsoft.AspNetCore.Authorization;

using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// User management endpoints for API version 1.0.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[Authorize]
public class UserController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    /// <summary>
    /// Initializes a new instance of the UserController class.
    /// </summary>
    /// <param name="userService">User service</param>
    /// <param name="logger">Logger instance</param>
    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Update the authenticated user's profile information.
    /// </summary>
    /// <remarks>
    /// Partially updates the current user's profile information. This is a selective update - only provide
    /// the fields you want to change. Unprovided fields will remain unchanged.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Required Fields:**
    /// - **CurrentPassword**: Current password for verification (always required for security)
    ///
    /// **Optional Fields (only update if provided):**
    /// - **Name**: User's full name (max 100 characters, leave null to keep current name)
    /// - **Email**: Valid email address (max 254 characters, leave null to keep current email)
    /// - **NewPassword**: New password (8-100 characters, leave null to keep current password)
    ///   - Must contain at least one uppercase letter
    ///   - Must contain at least one lowercase letter
    ///   - Must contain at least one digit
    ///   - Must contain at least one special character
    /// - **InitialBalance**: Account balance (leave null to keep current balance)
    ///
    /// **How It Works:**
    /// - Provide only the fields you want to update along with CurrentPassword
    /// - All unprovided (null) fields remain unchanged
    /// - You can update a single field or multiple fields in one request
    ///
    /// **Security Notes:**
    /// - Current password must always be provided to verify user identity
    /// - If changing email, the new email must not already be in use by another user
    /// - Password changes require meeting all strength requirements
    ///
    /// **Use Cases:**
    /// - Update only name: Provide CurrentPassword and Name
    /// - Update only email: Provide CurrentPassword and Email
    /// - Update only password: Provide CurrentPassword and NewPassword
    /// - Update only balance: Provide CurrentPassword and InitialBalance
    /// - Update multiple fields: Provide CurrentPassword and any combination of fields
    /// </remarks>
    /// <param name="request">User profile update request with CurrentPassword (required) and optional fields to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Updated user profile information with updated timestamp</returns>
    /// <response code="200">User profile updated successfully - returns complete updated user information</response>
    /// <response code="400">Invalid request data or validation errors (e.g., weak new password, invalid email format, missing current password)</response>
    /// <response code="401">User not authenticated or current password is incorrect</response>
    /// <response code="409">Email already exists for another user account</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UpdateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();
        var result = await _userService.UpdateAsync(userId, request, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to update profile for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var response = result.Value;
        return Ok(response);
    }

    /// <summary>
    /// Delete the authenticated user's account (permanent deletion).
    /// </summary>
    /// <remarks>
    /// Permanently deletes the authenticated user's account and all associated data.
    /// This action is irreversible and cannot be undone.
    ///
    /// **Required Fields:**
    /// - **CurrentPassword**: Current password for verification (required for security)
    /// - **ConfirmDeletion**: Must be set to `true` to confirm deletion intent
    ///
    /// **What Gets Deleted:**
    /// - User account and profile information
    /// - All transactions associated with the user
    /// - All transaction groups
    /// - All custom categories
    /// - Authentication tokens become invalid
    ///
    /// **Security Notes:**
    /// - Current password must be provided to verify user identity
    /// - Confirmation flag must be explicitly set to true
    /// - User must be authenticated via JWT token
    /// - Action is logged for audit purposes
    ///
    /// **⚠️ WARNING: This action is permanent and cannot be undone!**
    /// </remarks>
    /// <param name="request">Delete request containing current password for verification and explicit confirmation flag</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Delete confirmation response with user details and success message</returns>
    /// <response code="200">User account deleted successfully - returns confirmation with user details</response>
    /// <response code="400">Invalid request data, validation errors, or missing deletion confirmation</response>
    /// <response code="401">User not authenticated or current password is incorrect</response>
    [HttpDelete("profile")]
    [ProducesResponseType(typeof(DeleteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteProfile(
        [FromBody] DeleteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();
        var result = await _userService.DeleteAsync(userId, request, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to delete account for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var response = result.Value;
        return Ok(response);
    }
}
