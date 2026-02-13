using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Controllers;
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
    /// <param name="request">User profile update details including current password for verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile information</returns>
    /// <response code="200">User profile updated successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">User not authenticated or invalid current password</response>
    /// <response code="409">Email already exists for another user</response>
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
    /// <param name="request">Delete request with current password verification and confirmation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete confirmation response</returns>
    /// <response code="200">User account deleted successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">User not authenticated or invalid current password</response>
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
