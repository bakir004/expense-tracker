using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Transactions;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using ExpenseTrackerAPI.Domain.Errors;

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
    /// Get transactions for the authenticated user with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="transactionType">Filter by type: EXPENSE or INCOME</param>
    /// <param name="minAmount">Filter by minimum amount (inclusive)</param>
    /// <param name="maxAmount">Filter by maximum amount (inclusive)</param>
    /// <param name="dateFrom">Filter by start date (yyyy-MM-dd format, inclusive)</param>
    /// <param name="dateTo">Filter by end date (yyyy-MM-dd format, inclusive)</param>
    /// <param name="subjectContains">Filter by subject containing text (case-insensitive)</param>
    /// <param name="notesContains">Filter by notes containing text (case-insensitive)</param>
    /// <param name="paymentMethods">Filter by payment methods (comma-separated: CASH,DEBIT_CARD,CREDIT_CARD,BANK_TRANSFER,MOBILE_PAYMENT,PAYPAL,CRYPTO,OTHER)</param>
    /// <param name="categoryIds">Filter by category IDs (comma-separated)</param>
    /// <param name="uncategorized">Filter for uncategorized transactions only</param>
    /// <param name="transactionGroupIds">Filter by transaction group IDs (comma-separated)</param>
    /// <param name="ungrouped">Filter for ungrouped transactions only</param>
    /// <param name="sortBy">Sort field: date, amount, subject, paymentMethod, createdAt, updatedAt (default: date)</param>
    /// <param name="sortDirection">Sort direction: asc or desc (default: desc)</param>
    /// <param name="page">Page number, 1-based (default: 1)</param>
    /// <param name="pageSize">Items per page, max 100 (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of transactions matching the filter criteria</returns>
    /// <response code="200">Transactions retrieved successfully</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(TransactionFilterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? transactionType = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? subjectContains = null,
        [FromQuery] string? notesContains = null,
        [FromQuery] string? paymentMethods = null,
        [FromQuery] string? categoryIds = null,
        [FromQuery] bool? uncategorized = null,
        [FromQuery] string? transactionGroupIds = null,
        [FromQuery] bool? ungrouped = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        // Build filter request from query parameters
        var filterRequest = new TransactionFilterRequest
        {
            TransactionType = transactionType,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SubjectContains = subjectContains,
            NotesContains = notesContains,
            PaymentMethods = ParseCommaSeparatedArray(paymentMethods),
            CategoryIds = ParseCommaSeparatedIntArray(categoryIds),
            Uncategorized = uncategorized,
            TransactionGroupIds = ParseCommaSeparatedIntArray(transactionGroupIds),
            Ungrouped = ungrouped,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Page = page,
            PageSize = pageSize
        };

        // Parse and validate the filter request
        var parseResult = TransactionFilterParser.Parse(filterRequest);
        if (parseResult.IsError)
        {
            _logger.LogWarning("Invalid filter parameters for user {UserId}: {Errors}",
                userId, string.Join(", ", parseResult.Errors.Select(e => e.Description)));
            return Problem(parseResult.Errors);
        }

        var filter = parseResult.Value;

        var result = await _transactionService.GetByUserIdWithFilterAsync(userId, filter, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get transactions for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        return Ok(result.Value);
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
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction ID {TransactionId} provided by user {UserId}", id, GetUserId());
            return Problem(TransactionErrors.InvalidTransactionId);
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
        return Ok(transaction.ToResponse());
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
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
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
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id, version = "1.0" }, transaction.ToResponse());
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
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransaction(
        int id,
        [FromBody] UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction ID {TransactionId} provided by user {UserId}", id, GetUserId());
            return Problem(TransactionErrors.InvalidTransactionId);
        }

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
        return Ok(transaction.ToResponse());
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
        if (id <= 0)
        {
            _logger.LogWarning("Invalid transaction ID {TransactionId} provided by user {UserId}", id, GetUserId());
            return Problem(TransactionErrors.InvalidTransactionId);
        }

        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionService.DeleteAsync(id, userId, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to delete transaction {TransactionId} for user {UserId}: {Errors}",
                id, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Parses a comma-separated string into a string array.
    /// </summary>
    private static string[]? ParseCommaSeparatedArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Parses a comma-separated string of integers into an int array.
    /// </summary>
    private static int[]? ParseCommaSeparatedIntArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<int>();

        foreach (var part in parts)
        {
            if (int.TryParse(part, out var intValue))
            {
                result.Add(intValue);
            }
        }

        return result.Count > 0 ? result.ToArray() : null;
    }
}
