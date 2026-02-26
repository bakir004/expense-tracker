using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Transactions;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using ExpenseTrackerAPI.Domain.Errors;
using Microsoft.Extensions.Options;
using ExpenseTrackerAPI.WebApi.Configuration;

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
    private readonly ApiSettings _apiSettings;

    /// <summary>
    /// Initializes a new instance of the TransactionController class.
    /// </summary>
    /// <param name="transactionService">Transaction service</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="apiSettings">API configuration settings</param>
    public TransactionController(
            ITransactionService transactionService,
            ILogger<TransactionController> logger,
            IOptions<ApiSettings> apiSettings)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiSettings = apiSettings?.Value ?? throw new ArgumentNullException(nameof(apiSettings));
    }

    /// <summary>
    /// Get transactions for the authenticated user with optional filtering, sorting, and pagination.
    /// </summary>
    /// <remarks>
    /// Retrieves transactions with powerful filtering, sorting, and pagination capabilities. Use this endpoint
    /// to query expenses and income with various criteria for reporting, budgeting, and analysis.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Filtering Options:**
    /// - **transactionType**: Filter by EXPENSE or INCOME
    /// - **minAmount/maxAmount**: Filter by amount range (inclusive)
    /// - **dateFrom/dateTo**: Filter by date range in yyyy-MM-dd format (inclusive)
    /// - **subjectContains**: Search in transaction subjects (case-insensitive)
    /// - **notesContains**: Search in transaction notes (case-insensitive)
    /// - **paymentMethods**: Comma-separated list (CASH, DEBIT_CARD, CREDIT_CARD, BANK_TRANSFER, MOBILE_PAYMENT, PAYPAL, CRYPTO, OTHER)
    /// - **categoryIds**: Comma-separated category IDs
    /// - **uncategorized**: Set to true to get only transactions without categories
    /// - **transactionGroupIds**: Comma-separated transaction group IDs
    /// - **ungrouped**: Set to true to get only transactions without groups
    ///
    /// **Sorting:**
    /// - **sortBy**: date, amount, subject, categoryId, transactionGroupId, paymentMethod, createdAt, updatedAt (default: date)
    /// - **sortDirection**: asc or desc (default: desc)
    ///
    /// **Pagination:**
    /// - **page**: Page number starting from 1 (default: 1)
    /// - **pageSize**: Items per page, max 100 (default: 20)
    ///
    /// **Response Fields:**
    /// - **transactions**: Array of transaction objects
    /// - **totalCount**: Total number of transactions matching filters
    /// - **page**: Current page number
    /// - **pageSize**: Items per page
    /// - **totalPages**: Total number of pages
    ///
    /// **Use Cases:**
    /// - Viewing all expenses for a specific month
    /// - Filtering transactions by category for budget analysis
    /// - Searching for specific transactions by subject
    /// - Generating reports for specific date ranges
    /// - Tracking cash flow by payment method
    ///
    /// **Note:** Only transactions belonging to the authenticated user are returned.
    /// </remarks>
    /// <param name="transactionType">Filter by transaction type: EXPENSE or INCOME</param>
    /// <param name="minAmount">Filter by minimum amount (inclusive, e.g., 10.00)</param>
    /// <param name="maxAmount">Filter by maximum amount (inclusive, e.g., 1000.00)</param>
    /// <param name="dateFrom">Filter by start date in yyyy-MM-dd format (inclusive, e.g., 2024-01-01)</param>
    /// <param name="dateTo">Filter by end date in yyyy-MM-dd format (inclusive, e.g., 2024-12-31)</param>
    /// <param name="subjectContains">Filter by subject containing this text (case-insensitive)</param>
    /// <param name="notesContains">Filter by notes containing this text (case-insensitive)</param>
    /// <param name="paymentMethods">Filter by payment methods as comma-separated values (e.g., CASH,DEBIT_CARD,CREDIT_CARD)</param>
    /// <param name="categoryIds">Filter by category IDs as comma-separated values (e.g., 1,2,3)</param>
    /// <param name="uncategorized">Set to true to filter for transactions without a category</param>
    /// <param name="transactionGroupIds">Filter by transaction group IDs as comma-separated values (e.g., 1,2,3)</param>
    /// <param name="ungrouped">Set to true to filter for transactions without a transaction group</param>
    /// <param name="sortBy">Field to sort by: date, amount, subject, categoryId, transactionGroupid, paymentMethod, createdAt, updatedAt (default: date)</param>
    /// <param name="sortDirection">Sort direction: asc for ascending, desc for descending (default: desc)</param>
    /// <param name="page">Page number starting from 1 (default: 1)</param>
    /// <param name="pageSize">Number of items per page, maximum 50 (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Paginated list of transactions matching the filter criteria with pagination metadata</returns>
    /// <response code="200">Transactions retrieved successfully - returns paginated transaction list</response>
    /// <response code="400">Invalid filter parameters (e.g., invalid date format, invalid enum values)</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
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

        var parseResult = TransactionFilterParser.Parse(filterRequest, _apiSettings.MaxPageSize, _apiSettings.DefaultPageSize);
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
    /// <remarks>
    /// Retrieves detailed information about a specific transaction. The transaction must belong
    /// to the authenticated user - users cannot access transactions created by other users.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Security:**
    /// - Users can only retrieve their own transactions
    /// - Attempting to access another user's transaction will return 404 Not Found
    /// - Transaction ownership is verified before returning data
    ///
    /// **Response Fields:**
    /// - **Id**: Transaction unique identifier
    /// - **TransactionType**: EXPENSE or INCOME
    /// - **Amount**: Absolute amount (always positive)
    /// - **SignedAmount**: Amount with sign (negative for expenses, positive for income)
    /// - **Date**: Transaction date
    /// - **Subject**: Brief description
    /// - **Notes**: Optional detailed notes
    /// - **PaymentMethod**: How the transaction was paid
    /// - **CumulativeDelta**: Running balance at this transaction
    /// - **CategoryId**: Associated category (if any)
    /// - **TransactionGroupId**: Associated group (if any)
    /// - **CreatedAt/UpdatedAt**: Timestamps
    ///
    /// **Use Cases:**
    /// - Viewing full details of a specific transaction
    /// - Retrieving transaction information before editing
    /// - Displaying transaction details in the UI
    ///
    /// **Note:** ID must be a positive integer. Invalid IDs will return a 400 Bad Request error.
    /// </remarks>
    /// <param name="id">The unique identifier of the transaction to retrieve (must be positive integer)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Transaction details including all fields and related information</returns>
    /// <response code="200">Transaction retrieved successfully - returns transaction object</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Transaction not found or does not belong to authenticated user</response>
    /// <response code="500">Internal server error - unexpected error occurred</response>
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

        var result = await _transactionService.GetByIdAsync(id, userId, cancellationToken);


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
    /// <remarks>
    /// Creates a new expense or income transaction. Transactions are the core entities for tracking
    /// financial activity and can be categorized and grouped for better organization.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Required Fields:**
    /// - **TransactionType**: EXPENSE or INCOME
    /// - **Amount**: Positive decimal value (e.g., 50.00)
    /// - **Date**: Transaction date in yyyy-MM-dd format
    /// - **Subject**: Brief description (1-255 characters)
    /// - **PaymentMethod**: CASH, DEBIT_CARD, CREDIT_CARD, BANK_TRANSFER, MOBILE_PAYMENT, PAYPAL, CRYPTO, or OTHER
    ///
    /// **Optional Fields:**
    /// - **Notes**: Detailed description (max 2000 characters)
    /// - **CategoryId**: ID of existing category (e.g., 1 for Food)
    /// - **TransactionGroupId**: ID of existing transaction group
    ///
    /// **Transaction Types:**
    /// - **EXPENSE**: Money going out (e.g., purchases, bills, payments)
    /// - **INCOME**: Money coming in (e.g., salary, refunds, gifts)
    ///
    /// **Payment Methods:**
    /// - CASH, DEBIT_CARD, CREDIT_CARD, BANK_TRANSFER, MOBILE_PAYMENT, PAYPAL, CRYPTO, OTHER
    ///
    /// **Response:**
    /// Returns the created transaction with auto-generated ID, timestamps, and calculated fields
    /// like SignedAmount and CumulativeDelta.
    ///
    /// **Use Cases:**
    /// - Recording daily expenses
    /// - Logging income transactions
    /// - Tracking business expenses with categories
    /// - Grouping related transactions (e.g., vacation expenses)
    ///
    /// **Note:** If CategoryId or TransactionGroupId is provided, they must reference existing entities
    /// owned by the authenticated user.
    /// </remarks>
    /// <param name="request">Transaction creation request containing all required fields and optional category/group associations</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Created transaction with ID, timestamps, and calculated fields</returns>
    /// <response code="201">Transaction created successfully - returns created transaction</response>
    /// <response code="400">Invalid request data or validation errors (e.g., invalid transaction type, negative amount, invalid date format)</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Referenced entity not found (e.g., category or transaction group does not exist or does not belong to user)</response>
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
    /// <remarks>
    /// Updates all fields of an existing transaction. Only the transaction owner can update the transaction.
    /// This is a full update operation - all fields must be provided even if not changing.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Required Fields:**
    /// - **TransactionType**: EXPENSE or INCOME
    /// - **Amount**: Positive decimal value (e.g., 75.00)
    /// - **Date**: Transaction date in yyyy-MM-dd format
    /// - **Subject**: Brief description (1-255 characters)
    /// - **PaymentMethod**: CASH, DEBIT_CARD, CREDIT_CARD, BANK_TRANSFER, MOBILE_PAYMENT, PAYPAL, CRYPTO, or OTHER
    ///
    /// **Optional Fields:**
    /// - **Notes**: Detailed description (max 2000 characters, set to null to remove)
    /// - **CategoryId**: ID of existing category (set to null to remove category)
    /// - **TransactionGroupId**: ID of existing transaction group (set to null to remove from group)
    ///
    /// **Security:**
    /// - Users can only update their own transactions
    /// - Attempting to update another user's transaction will return 404 Not Found
    /// - Transaction ownership is verified before applying changes
    ///
    /// **Use Cases:**
    /// - Correcting transaction amount or date
    /// - Updating transaction description
    /// - Changing payment method
    /// - Adding or removing category/group associations
    /// - Fixing data entry errors
    ///
    /// **Note:** The transaction ID in the URL must match an existing transaction owned by the authenticated user.
    /// ID must be a positive integer. If CategoryId or TransactionGroupId is provided, they must reference
    /// existing entities owned by the user.
    /// </remarks>
    /// <param name="id">The unique identifier of the transaction to update (must be positive integer)</param>
    /// <param name="request">Transaction update request containing all fields to be updated</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Updated transaction with new values and updated timestamp</returns>
    /// <response code="200">Transaction updated successfully - returns updated transaction object</response>
    /// <response code="400">Invalid request data or validation errors (e.g., invalid transaction type, negative amount, invalid ID)</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Transaction not found, does not belong to authenticated user, or referenced entity not found</response>
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
    /// <remarks>
    /// Permanently deletes a transaction. Only the transaction owner can delete the transaction.
    /// This action cannot be undone.
    ///
    /// **Authentication:** Required - must include valid JWT token in Authorization header
    ///
    /// **Important:**
    /// - Deleting a transaction is permanent and cannot be undone
    /// - The transaction is completely removed from the system
    /// - This affects cumulative balance calculations for subsequent transactions
    /// - Category and group associations are removed
    ///
    /// **Security:**
    /// - Users can only delete their own transactions
    /// - Attempting to delete another user's transaction will return 404 Not Found
    /// - Transaction ownership is verified before deletion
    ///
    /// **Use Cases:**
    /// - Removing duplicate transactions
    /// - Deleting test or incorrect entries
    /// - Cleaning up old or irrelevant transactions
    ///
    /// **Response:**
    /// Returns 204 No Content on successful deletion with no response body.
    ///
    /// **Note:** ID must be a positive integer. Invalid IDs will return a 400 Bad Request error.
    /// </remarks>
    /// <param name="id">The unique identifier of the transaction to delete (must be positive integer)</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>No content on successful deletion</returns>
    /// <response code="204">Transaction deleted successfully - no response body</response>
    /// <response code="400">Invalid TransactionId</response>
    /// <response code="401">User not authenticated - valid JWT token required</response>
    /// <response code="404">Transaction not found or does not belong to authenticated user</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
    /// Batch delete transactions.
    /// </summary>
    /// <remarks>
    /// Permanently deletes a list of transactions. Only transactions owned by the authenticated user will be deleted.
    /// This action is atomic; if the service fails, no transactions are deleted.
    ///
    /// **Security:**
    /// - Ownership is verified for all provided IDs.
    /// - If any transaction does not belong to the user, the operation will fail or skip based on service logic.
    /// </remarks>
    /// <param name="ids">A list of unique identifiers for the transactions to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <response code="204">Transactions deleted successfully</response>
    /// <response code="400">Invalid ID list or empty request</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">One or more transactions not found</response>
    [HttpDelete("batch")] // Changed from {id:int} to a static "batch" route
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchDeleteTransactions(
        [FromBody] List<int> ids, 
        CancellationToken cancellationToken = default)
    {
        if (ids == null || !ids.Any() || ids.Any(id => id <= 0))
        {
            _logger.LogWarning("Invalid batch delete request: IDs are empty or contain non-positive values. User: {UserId}", GetUserId());
            return Problem(TransactionErrors.InvalidTransactionId);
        }

        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionService.DeleteBatchAsync(ids, userId, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to batch delete {Count} transactions for user {UserId}: {Errors}",
                ids.Count, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return Problem(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Get aggregated transaction data for time-series charting.
    /// </summary>
    /// <remarks>
    /// Groups transactions by date and calculates daily totals for income, expenses, and net balance.
    /// Useful for building bar, line, or area charts showing financial trends.
    /// 
    /// **Logic:**
    /// - **NetIncome**: Sum of all positive amounts for that day.
    /// - **NetExpenses**: Sum of all negative amounts for that day.
    /// - **NetAmount**: The final balance for the day (Income + Expenses).
    /// - **Transactions**: The full list of individual transaction details that occurred on that date.
    ///
    /// **Authentication:** Required (Bearer Token).
    /// </remarks>
    /// <param name="request">The date range for the chart data (yyyy-MM-dd).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the aggregated chart data points sorted by date ascending.</response>
    /// <response code="400">If the date range is invalid (e.g., dateFrom is after dateTo).</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpPost("net-chart-data")] 
    [ProducesResponseType(typeof(TransactionNetChartDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactionNetChartData(
        [FromBody] GetTransactionNetChartDataRequest request, 
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        var result = await _transactionService.GetTransactionNetChartDataAsync(userId, request, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get transaction net chart data for user {UserId}: {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get aggregated transaction data by category.
    /// </summary>
    /// <remarks>
    /// Groups transactions by CategoryId and calculates total spending per category.
    /// Useful for pie charts or horizontal bar charts showing spending distribution.
    /// 
    /// **Authentication:** Required (Bearer Token).
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the aggregated category data.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet("category-chart-data")] // Changed to GET
    [ProducesResponseType(typeof(TransactionByCategoryChartDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactionByCategoryChartData(
        CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var userId = GetRequiredUserId();

        // Calling the new Service method we just wrote
        var result = await _transactionService.GetTransactionByCategoryChartDataAsync(userId, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get category chart data for user {UserId}: {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        return Ok(result.Value);
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
