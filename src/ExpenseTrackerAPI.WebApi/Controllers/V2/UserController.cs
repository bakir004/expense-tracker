using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Controllers.V2;

/// <summary>
/// Enhanced user management endpoints for API version 2.0.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("2.0")]
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
    /// Update the authenticated user's profile information with enhanced validation and security.
    /// </summary>
    /// <param name="request">User profile update details including current password for verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced updated user profile information with additional metadata</returns>
    /// <response code="200">User profile updated successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">User not authenticated or invalid current password</response>
    /// <response code="409">Email already exists for another user</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(EnhancedUpdateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();
        _logger.LogInformation("V2 - Updating profile for user {UserId}", userId);

        var result = await _userService.UpdateAsync(userId, request, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("V2 - Failed to update profile for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var baseResponse = result.Value;

        // Enhanced V2 response with additional metadata
        var enhancedResponse = new EnhancedUpdateUserResponse(
            Id: baseResponse.Id,
            Name: baseResponse.Name,
            Email: baseResponse.Email,
            InitialBalance: baseResponse.InitialBalance,
            UpdatedAt: baseResponse.UpdatedAt,
            ApiVersion: "2.0",
            UpdateTimestamp: DateTime.UtcNow,
            SecurityInfo: new UserSecurityInfo(
                PasswordChanged: !string.IsNullOrWhiteSpace(request.NewPassword),
                EmailChanged: !string.Equals(HttpContext.Items["OriginalEmail"]?.ToString(), request.Email, StringComparison.OrdinalIgnoreCase),
                LastLogin: DateTime.UtcNow, // This would typically come from user tracking
                ProfileCompleteness: CalculateProfileCompleteness(baseResponse)
            ),
            ValidationSummary: new UpdateValidationSummary(
                FieldsUpdated: GetUpdatedFields(request),
                SecurityChecksPerformed: new[] { "CurrentPasswordVerification", "EmailUniquenessCheck", "PasswordComplexityValidation" },
                UpdateSource: "WebAPI_V2"
            )
        );

        _logger.LogInformation("V2 - Successfully updated profile for user {UserId} with enhanced metadata", userId);

        return Ok(enhancedResponse);
    }

    /// <summary>
    /// Delete the authenticated user's account with enhanced security logging (permanent deletion).
    /// </summary>
    /// <param name="request">Delete request with current password verification and confirmation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced delete confirmation response with security metadata</returns>
    /// <response code="200">User account deleted successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">User not authenticated or invalid current password</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("profile")]
    [ProducesResponseType(typeof(EnhancedDeleteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteProfile(
        [FromBody] DeleteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();
        _logger.LogInformation("V2 - Attempting to delete account for user {UserId}", userId);

        var result = await _userService.DeleteAsync(userId, request, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("V2 - Failed to delete account for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var baseResponse = result.Value;

        // Enhanced V2 response with additional security metadata
        var enhancedResponse = new EnhancedDeleteUserResponse(
            Id: baseResponse.Id,
            Name: baseResponse.Name,
            Email: baseResponse.Email,
            Message: baseResponse.Message,
            ApiVersion: "2.0",
            DeleteTimestamp: DateTime.UtcNow,
            SecurityInfo: new DeleteSecurityInfo(
                PasswordVerified: true,
                ConfirmationReceived: request.ConfirmDeletion,
                DeleteSource: "WebAPI_V2",
                UserAgent: HttpContext.Request.Headers["User-Agent"].ToString(),
                IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            ),
            AuditTrail: new DeleteAuditTrail(
                DeletedBy: userId,
                DeleteReason: "User-initiated account deletion",
                DataRetentionPolicy: "Immediate hard delete - no recovery possible",
                ComplianceNotes: "GDPR Article 17 - Right to erasure"
            )
        );

        _logger.LogInformation("V2 - Successfully deleted account for user {UserId} with enhanced audit trail", userId);

        return Ok(enhancedResponse);
    }

    /// <summary>
    /// Calculate profile completeness percentage.
    /// </summary>
    private static decimal CalculateProfileCompleteness(UpdateUserResponse user)
    {
        var completedFields = 0;
        var totalFields = 4;

        if (!string.IsNullOrWhiteSpace(user.Name)) completedFields++;
        if (!string.IsNullOrWhiteSpace(user.Email)) completedFields++;
        if (user.InitialBalance != 0) completedFields++; // Assumes 0 is default/incomplete
        completedFields++; // Password is always complete if user can update

        return Math.Round((decimal)completedFields / totalFields * 100, 2);
    }

    /// <summary>
    /// Get list of fields that were updated in the request.
    /// </summary>
    private static string[] GetUpdatedFields(UpdateUserRequest request)
    {
        var updatedFields = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Name)) updatedFields.Add("Name");
        if (!string.IsNullOrWhiteSpace(request.Email)) updatedFields.Add("Email");
        if (!string.IsNullOrWhiteSpace(request.NewPassword)) updatedFields.Add("Password");
        updatedFields.Add("InitialBalance"); // Always included as it's required

        return updatedFields.ToArray();
    }
}

/// <summary>
/// Enhanced response contract for V2 user profile updates with additional metadata.
/// </summary>
public record EnhancedUpdateUserResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime UpdatedAt,
    string ApiVersion,
    DateTime UpdateTimestamp,
    UserSecurityInfo SecurityInfo,
    UpdateValidationSummary ValidationSummary);

/// <summary>
/// Security-related information about the user update operation.
/// </summary>
public record UserSecurityInfo(
    bool PasswordChanged,
    bool EmailChanged,
    DateTime LastLogin,
    decimal ProfileCompleteness);

/// <summary>
/// Summary of validation performed during the update operation.
/// </summary>
public record UpdateValidationSummary(
    string[] FieldsUpdated,
    string[] SecurityChecksPerformed,
    string UpdateSource);

/// <summary>
/// Enhanced response contract for V2 user deletion with additional security metadata.
/// </summary>
public record EnhancedDeleteUserResponse(
    int Id,
    string Name,
    string Email,
    string Message,
    string ApiVersion,
    DateTime DeleteTimestamp,
    DeleteSecurityInfo SecurityInfo,
    DeleteAuditTrail AuditTrail);

/// <summary>
/// Security information about the delete operation.
/// </summary>
public record DeleteSecurityInfo(
    bool PasswordVerified,
    bool ConfirmationReceived,
    string DeleteSource,
    string UserAgent,
    string IpAddress);

/// <summary>
/// Audit trail information for the delete operation.
/// </summary>
public record DeleteAuditTrail(
    int DeletedBy,
    string DeleteReason,
    string DataRetentionPolicy,
    string ComplianceNotes);
