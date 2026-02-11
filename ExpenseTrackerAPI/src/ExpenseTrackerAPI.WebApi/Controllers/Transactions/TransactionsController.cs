using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Application.Transactions.Mappings;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.WebApi;
using ExpenseTrackerAPI.WebApi.Controllers;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.WebApi.Controllers.Transactions;

[ApiController]
[Route(ApiRoutes.V1Routes.Transactions)]
public class TransactionsController : ApiControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Get all transactions (admin/debug use)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetAllAsync(cancellationToken);

        return result.Match(
            transactions => Ok(transactions.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Get a single transaction by ID
    /// </summary>
    /// <summary>
    /// Get a transaction by ID
    /// </summary>
    /// <param name="id">The unique identifier of the transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction</returns>
    /// <response code="200">Transaction found</response>
    /// <response code="404">Transaction not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetByIdAsync(id, cancellationToken);

        return result.Match(
            transaction => Ok(transaction.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Get all transactions for a user, with optional filtering and sorting via query parameters.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="query">Optional: subject (fuzzy), categoryIds, paymentMethods, transactionType, dateFrom, dateTo, sortBy (subject|paymentMethod|category|amount), sortDirection (asc|desc). Date is always primary sort.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions for the user (empty array if user has no transactions)</returns>
    /// <response code="200">Successfully retrieved transactions (may be empty)</response>
    /// <response code="400">Invalid query parameter value</response>
    /// <response code="404">User not found</response>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(GetTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(
        int userId,
        [FromQuery] TransactionQueryParameters? query,
        CancellationToken cancellationToken)
    {
        var optionsResult = TransactionValidator.BuildQueryOptions(query);
        if (optionsResult.IsError)
        {
            return Problem(optionsResult.Errors);
        }

        var result = await _transactionService.GetByUserIdWithFiltersAsync(userId, optionsResult.Value, cancellationToken);

        return result.Match(
            transactions => Ok(transactions.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Get transactions for a user filtered by type (expenses or income)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="type">Transaction type: 'EXPENSE' or 'INCOME'</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions for the user filtered by type (empty array if user has no transactions of that type)</returns>
    /// <response code="200">Successfully retrieved transactions (may be empty)</response>
    /// <response code="400">Invalid transaction type</response>
    /// <response code="404">User not found</response>
    [HttpGet("user/{userId:int}/type/{type}")]
    [ProducesResponseType(typeof(GetTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserIdAndType(int userId, string type, CancellationToken cancellationToken)
    {
        var typeResult = TransactionValidator.ParseTransactionType(type);
        if (typeResult.IsError)
        {
            return Problem(typeResult.Errors);
        }

        var result = await _transactionService.GetByUserIdAndTypeAsync(userId, typeResult.Value, cancellationToken);

        return result.Match(
            transactions => Ok(transactions.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Get transactions for a user within a date range
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="from">Start date in yyyy-MM-dd format (e.g. 2024-01-01)</param>
    /// <param name="to">End date in yyyy-MM-dd format (e.g. 2024-12-31)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions for the user within the date range</returns>
    /// <response code="200">Successfully retrieved transactions (may be empty)</response>
    /// <response code="400">Invalid date format</response>
    /// <response code="404">User not found</response>
    [HttpGet("user/{userId:int}/range")]
    [ProducesResponseType(typeof(GetTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserIdAndDateRange(
        int userId,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken cancellationToken)
    {
        const string dateFormat = "yyyy-MM-dd";

        if (!DateOnly.TryParseExact(from, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
        {
            return BadRequest(new { error = $"Invalid 'from' date format. Expected format: {dateFormat}" });
        }

        if (!DateOnly.TryParseExact(to, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            return BadRequest(new { error = $"Invalid 'to' date format. Expected format: {dateFormat}" });
        }

        if (startDate > endDate)
        {
            return BadRequest(new { error = "'from' date must be before or equal to 'to' date" });
        }

        var result = await _transactionService.GetByUserIdAndDateRangeAsync(userId, startDate, endDate, cancellationToken);

        return result.Match(
            transactions => Ok(transactions.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    /// <param name="request">Transaction creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created transaction</returns>
    /// <response code="201">Transaction created successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="404">User, category, or transaction group not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var transactionType = Enum.Parse<TransactionType>(request.TransactionType);
        var date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var paymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod);

        var result = await _transactionService.CreateAsync(
            request.UserId,
            transactionType,
            request.Amount,
            date,
            request.Subject,
            request.Notes,
            paymentMethod,
            request.CategoryId,
            request.TransactionGroupId,
            request.IncomeSource,
            cancellationToken);

        return result.Match(
            transaction => CreatedAtAction(nameof(GetById), new { id = transaction.Id }, transaction.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to update</param>
    /// <param name="request">Transaction update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated transaction</returns>
    /// <response code="200">Transaction updated successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="404">Transaction, category, or transaction group not found</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var transactionType = Enum.Parse<TransactionType>(request.TransactionType);
        var date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var paymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod);

        var result = await _transactionService.UpdateAsync(
            id,
            transactionType,
            request.Amount,
            date,
            request.Subject,
            request.Notes,
            paymentMethod,
            request.CategoryId,
            request.TransactionGroupId,
            request.IncomeSource,
            cancellationToken);

        return result.Match(
            transaction => Ok(transaction.ToResponse()),
            Problem);
    }

    /// <summary>
    /// Delete a transaction
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Transaction deleted successfully</response>
    /// <response code="404">Transaction not found</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _transactionService.DeleteAsync(id, cancellationToken);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

