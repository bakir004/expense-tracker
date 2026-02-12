using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// Transaction management endpoints for API version 1.0.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
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
    /// Get all transactions for the authenticated user with pagination.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Number of items per page (1-100, default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of transactions</returns>
    /// <response code="200">Transactions retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid or missing user ID in token claims");
            return Unauthorized();
        }

        _logger.LogInformation("Getting transactions for user {UserId}, page {PageNumber}, size {PageSize}",
            userId, pageNumber, pageSize);

        var result = await _transactionService.GetAllTransactionsAsync(userId, pageNumber, pageSize, cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get transactions for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var response = result.Value;
        _logger.LogInformation("Successfully retrieved {Count} transactions for user {UserId} (page {PageNumber}/{TotalPages})",
            response.Transactions.Count(), userId, pageNumber,
            (int)Math.Ceiling((double)response.TotalCount / pageSize));

        return Ok(response);
    }
}
