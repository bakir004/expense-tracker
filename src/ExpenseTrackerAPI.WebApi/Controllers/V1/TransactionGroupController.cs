using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.TransactionGroups;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// Transaction group management endpoints for API version 1.0.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/transaction-groups")]
[ApiVersion("1.0")]
[Authorize]
public class TransactionGroupController : ApiControllerBase
{
    private readonly ITransactionGroupService _transactionGroupService;
    private readonly ILogger<TransactionGroupController> _logger;

    /// <summary>
    /// Initializes a new instance of the TransactionGroupController class.
    /// </summary>
    /// <param name="transactionGroupService">Transaction group service</param>
    /// <param name="logger">Logger instance</param>
    public TransactionGroupController(
        ITransactionGroupService transactionGroupService,
        ILogger<TransactionGroupController> logger)
    {
        _transactionGroupService = transactionGroupService ?? throw new ArgumentNullException(nameof(transactionGroupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all transaction groups for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Retrieves all transaction groups created by the authenticated user. Transaction groups are
    /// organizational containers that allow users to group related transactions together for better
    /// tracking and reporting (e.g., "Monthly Expenses", "Vacation Budget", "Home Renovation").
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **What are Transaction Groups?**
    /// Transaction groups help organize transactions into logical collections. Each group can contain
    /// multiple transactions and provides a way to track related expenses or income together.
    ///
    /// **Response Fields:**
    /// - **Id**: Unique identifier for the transaction group
    /// - **Name**: Group name (e.g., "January Budget", "Trip to Paris")
    /// - **Description**: Optional detailed description of the group's purpose
    /// - **UserId**: ID of the user who owns this group
    /// - **CreatedAt**: UTC timestamp when the group was created
    ///
    /// **Use Cases:**
    /// - Budget tracking by project or time period
    /// - Organizing expenses for specific events or trips
    /// - Grouping business expenses by client or project
    /// - Separating personal and business transactions
    ///
    /// **Note:** Only transaction groups belonging to the authenticated user are returned.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>List of all transaction groups belonging to the authenticated user</returns>
    /// <response code="200">Transaction groups retrieved successfully - returns array of group objects</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionGroupService.GetByUserIdAsync(userId, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get transaction groups for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transactionGroups = result.Value;
        return Ok(transactionGroups.ToResponses());
    }

    /// <summary>
    /// Get a specific transaction group by ID.
    /// </summary>
    /// <remarks>
    /// Retrieves detailed information about a specific transaction group. The group must belong
    /// to the authenticated user - users cannot access groups created by other users.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Security:**
    /// - Users can only retrieve their own transaction groups
    /// - Attempting to access another user's group will return 404 Not Found
    /// - Group ownership is verified before returning data
    ///
    /// **Use Cases:**
    /// - Viewing details of a specific transaction group
    /// - Retrieving group information before editing
    /// - Displaying group details in the UI
    ///
    /// **Note:** ID must be a positive integer. Invalid IDs will return a 400 Bad Request error.
    /// </remarks>
    /// <param name="id">The unique identifier of the transaction group to retrieve (must be positive integer)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Transaction group details including name, description, owner, and creation date</returns>
    /// <response code="200">Transaction group retrieved successfully - returns group object</response>
    /// <response code="400">Invalid TransactionGroupId</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Transaction group not found or does not belong to authenticated user</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction group ID {TransactionGroupId} provided by user {UserId}", id, GetUserId());
            return Problem(TransactionGroupErrors.InvalidTransactionGroupId);
        }

        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionGroupService.GetByIdAsync(id, userId, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get transaction group {TransactionGroupId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transactionGroup = result.Value;
        return Ok(transactionGroup.ToResponse());
    }

    /// <summary>
    /// Create a new transaction group for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Creates a new transaction group for organizing related transactions. The group is automatically
    /// associated with the authenticated user and can be used to categorize transactions.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Required Fields:**
    /// - **Name**: Group name (cannot be empty)
    ///
    /// **Optional Fields:**
    /// - **Description**: Detailed description of the group's purpose
    ///
    /// **Use Cases:**
    /// - Creating a budget category for a specific project
    /// - Setting up a group for vacation expenses
    /// - Organizing business expenses by client
    /// - Tracking monthly or yearly budgets
    ///
    /// **Note:** Group names do not need to be unique - multiple groups can have the same name.
    /// </remarks>
    /// <param name="request">Transaction group creation request containing name and optional description</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Created transaction group with ID and timestamps</returns>
    /// <response code="201">Transaction group created successfully - returns created group with Location header</response>
    /// <response code="400">Invalid request data or validation errors (e.g., missing name)</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionGroupService.CreateAsync(
            userId: userId,
            name: request.Name,
            description: request.Description,
            cancellationToken: cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to create transaction group for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transactionGroup = result.Value;
        return CreatedAtAction(
            nameof(GetById),
            new { id = transactionGroup.Id, version = "1.0" },
            transactionGroup.ToResponse());
    }

    /// <summary>
    /// Update an existing transaction group.
    /// </summary>
    /// <remarks>
    /// Updates the name and/or description of an existing transaction group. Only the group owner
    /// can update the group. All transactions associated with the group remain unchanged.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Required Fields:**
    /// - **Name**: Updated group name (cannot be empty)
    ///
    /// **Optional Fields:**
    /// - **Description**: Updated description (can be null to remove description)
    ///
    /// **Security:**
    /// - Users can only update their own transaction groups
    /// - Attempting to update another user's group will return 404 Not Found
    /// - Group ownership is verified before applying changes
    ///
    /// **Use Cases:**
    /// - Renaming a group for clarity
    /// - Updating group description with additional details
    /// - Correcting typos in group information
    ///
    /// **Note:** The group ID in the URL must match an existing group owned by the authenticated user.
    /// ID must be a positive integer.
    /// </remarks>
    /// <param name="id">The unique identifier of the transaction group to update (must be positive integer)</param>
    /// <param name="request">Transaction group update request containing new name and optional description</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Updated transaction group with new values and updated timestamp</returns>
    /// <response code="200">Transaction group updated successfully - returns updated group object</response>
    /// <response code="400">Invalid request data or validation errors (e.g., empty name, invalid ID)</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Transaction group not found or does not belong to authenticated user</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTransactionGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction group ID {TransactionGroupId} provided by user {UserId}", id, GetUserId());
            return Problem(TransactionGroupErrors.InvalidTransactionGroupId);
        }

        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionGroupService.UpdateAsync(
            id: id,
            userId: userId,
            name: request.Name,
            description: request.Description,
            cancellationToken: cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to update transaction group {TransactionGroupId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transactionGroup = result.Value;
        return Ok(transactionGroup.ToResponse());
    }

    /// <summary>
    /// Delete a transaction group.
    /// </summary>
    /// <remarks>
    /// Permanently deletes a transaction group. Only the group owner can delete the group.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **⚠️ Important:**
    /// - Deleting a group does NOT delete the transactions within it
    /// - Transactions will remain in the system but will no longer be associated with the group (set to null id)
    /// - This action cannot be undone
    ///
    /// **Security:**
    /// - Users can only delete their own transaction groups
    /// - Attempting to delete another user's group will return 404 Not Found
    /// - Group ownership is verified before deletion
    ///
    /// **Use Cases:**
    /// - Removing obsolete or completed project groups
    /// - Cleaning up old budget categories
    /// - Removing accidentally created groups
    ///
    /// **Response:**
    /// Returns 204 No Content on successful deletion with no response body.
    ///
    /// **Note:** ID must be a positive integer. Invalid IDs will return a 400 Bad Request error.
    /// </remarks>
    /// <param name="id">The unique identifier of the transaction group to delete (must be positive integer)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>No content on successful deletion</returns>
    /// <response code="204">Transaction group deleted successfully - no response body</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Transaction group not found or does not belong to authenticated user</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction group ID {TransactionGroupId} provided by user {UserId}", id, GetUserId());
            return Problem(TransactionGroupErrors.InvalidTransactionGroupId);
        }

        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionGroupService.DeleteAsync(id, userId, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to delete transaction group {TransactionGroupId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        return NoContent();
    }
}
