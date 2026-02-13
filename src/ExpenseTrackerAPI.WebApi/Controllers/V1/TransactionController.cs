using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// Transaction management endpoints for API version 1.0.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/transactions")]
[ApiVersion("1.0")]
[Authorize]
public class TransactionController : ApiControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    /// <summary>
    /// Initializes a new instance of the TransactionController class.
    /// </summary>
    /// <param name="transactionService">Transaction service</param>
    /// <param name="logger">Logger instance</param>
    public TransactionController(
        ITransactionService transactionService,
        ILogger<TransactionController> logger)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get a specific transaction by ID.
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction details</returns>
    /// <response code="200">Transaction retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Transaction not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction ID {TransactionId} provided by user {UserId}", id, GetUserId());
            return BadRequest("Transaction ID must be a positive integer.");
        }

        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;
        var userId = GetRequiredUserId();
        var result = await _transactionService.GetByIdAsync(id, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get transaction {TransactionId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transaction = result.Value;
        return Ok(transaction);
    }

    /// <summary>
    /// Create a new transaction for the authenticated user.
    /// </summary>
    /// <param name="request">Transaction creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created transaction</returns>
    /// <response code="201">Transaction created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Referenced entity not found (user, category, etc.)</response>
    [HttpPost]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        // Parse and validate string values to enums
        var parseResult = TransactionRequestParser.ParseCreateRequest(request);
        if (parseResult.IsError)
        {
            _logger.LogWarning("Invalid transaction request for user {UserId}: {Errors}",
                userId, string.Join(", ", parseResult.Errors.Select(e => e.Description)));
            return Problem(parseResult.Errors);
        }

        var (transactionType, paymentMethod) = parseResult.Value;

        var result = await _transactionService.CreateAsync(
            userId: userId,
            transactionType: transactionType,
            amount: request.Amount,
            date: request.Date,
            subject: request.Subject,
            notes: request.Notes,
            paymentMethod: paymentMethod,
            categoryId: request.CategoryId,
            transactionGroupId: request.TransactionGroupId,
            cancellationToken: cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to create transaction for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transaction = result.Value;
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id, version = "1.0" }, transaction);
    }

    /// <summary>
    /// Update an existing transaction.
    /// </summary>
    /// <param name="id">Transaction ID to update</param>
    /// <param name="request">Transaction update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated transaction</returns>
    /// <response code="200">Transaction updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Transaction not found or referenced entity not found</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(
        int id,
        [FromBody] UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        // Parse and validate string values to enums
        var parseResult = TransactionRequestParser.ParseUpdateRequest(request);
        if (parseResult.IsError)
        {
            _logger.LogWarning("Invalid transaction update request for user {UserId}: {Errors}",
                userId, string.Join(", ", parseResult.Errors.Select(e => e.Description)));
            return Problem(parseResult.Errors);
        }

        var (transactionType, paymentMethod) = parseResult.Value;

        var result = await _transactionService.UpdateAsync(
            id: id,
            userId: userId,
            transactionType: transactionType,
            amount: request.Amount,
            date: request.Date,
            subject: request.Subject,
            notes: request.Notes,
            paymentMethod: paymentMethod,
            categoryId: request.CategoryId,
            transactionGroupId: request.TransactionGroupId,
            cancellationToken: cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to update transaction {TransactionId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var transaction = result.Value;
        return Ok(transaction);
    }

    /// <summary>
    /// Delete a transaction.
    /// </summary>
    /// <param name="id">Transaction ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Transaction deleted successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Transaction not found</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(int id, CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        // First verify the transaction exists and belongs to the user
        var existingResult = await _transactionService.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            _logger.LogWarning("Transaction {TransactionId} not found for user {UserId}: {Errors}",
                id, userId, string.Join(", ", existingResult.Errors.Select(e => e.Description)));
            return Problem(existingResult.Errors);
        }

        if (existingResult.Value.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete transaction {TransactionId} belonging to user {OwnerId}",
                userId, id, existingResult.Value.UserId);
            return NotFound();
        }

        var result = await _transactionService.DeleteAsync(id, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to delete transaction {TransactionId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        return NoContent();
    }
}
