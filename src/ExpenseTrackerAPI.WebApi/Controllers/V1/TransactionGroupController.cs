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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transaction groups belonging to the user</returns>
    /// <response code="200">Transaction groups retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
    /// <param name="id">Transaction group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction group details</returns>
    /// <response code="200">Transaction group retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Transaction group not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status200OK)]
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
    /// <param name="request">Transaction group creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created transaction group</returns>
    /// <response code="201">Transaction group created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
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
    /// <param name="id">Transaction group ID to update</param>
    /// <param name="request">Transaction group update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated transaction group</returns>
    /// <response code="200">Transaction group updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Transaction group not found</response>
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
    /// <param name="id">Transaction group ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Transaction group deleted successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Transaction group not found</response>
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
