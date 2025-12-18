using Microsoft.AspNetCore.Mvc;
using SampleCkWebApp.Application.Transactions;
using SampleCkWebApp.Application.Transactions.Interfaces.Application;
using SampleCkWebApp.Application.Transactions.Mappings;
using SampleCkWebApp.Contracts.Transactions;

namespace SampleCkWebApp.WebApi.Controllers.Transactions;

[ApiController]
[Route("transactions")]
public class TransactionsController : ControllerBase
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
        
        if (result.IsError)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Get a single transaction by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetByIdAsync(id, cancellationToken);
        
        if (result.IsError)
        {
            var error = result.Errors.First();
            if (error.Type == ErrorOr.ErrorType.NotFound)
            {
                return NotFound(new { error = error.Description });
            }
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Get all transactions for a user
    /// </summary>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetByUserId(int userId, CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetByUserIdAsync(userId, cancellationToken);
        
        if (result.IsError)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Get transactions for a user filtered by type (expenses or income)
    /// </summary>
    [HttpGet("user/{userId:int}/type/{type}")]
    public async Task<IActionResult> GetByUserIdAndType(int userId, string type, CancellationToken cancellationToken)
    {
        var typeResult = TransactionValidator.ParseTransactionType(type);
        if (typeResult.IsError)
        {
            return BadRequest(new { errors = typeResult.Errors.Select(e => e.Description) });
        }
        
        var result = await _transactionService.GetByUserIdAndTypeAsync(userId, typeResult.Value, cancellationToken);
        
        if (result.IsError)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Create a new transaction
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var typeResult = TransactionValidator.ParseTransactionType(request.TransactionType);
        if (typeResult.IsError)
        {
            return BadRequest(new { errors = typeResult.Errors.Select(e => e.Description) });
        }
        
        var result = await _transactionService.CreateAsync(
            request.UserId,
            typeResult.Value,
            request.Amount,
            request.Date,
            request.Subject,
            request.Notes,
            request.PaymentMethod,
            request.CategoryId,
            request.TransactionGroupId,
            request.IncomeSource,
            cancellationToken);
        
        if (result.IsError)
        {
            var error = result.Errors.First();
            if (error.Type == ErrorOr.ErrorType.Validation)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value.ToResponse());
    }

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var typeResult = TransactionValidator.ParseTransactionType(request.TransactionType);
        if (typeResult.IsError)
        {
            return BadRequest(new { errors = typeResult.Errors.Select(e => e.Description) });
        }
        
        var result = await _transactionService.UpdateAsync(
            id,
            typeResult.Value,
            request.Amount,
            request.Date,
            request.Subject,
            request.Notes,
            request.PaymentMethod,
            request.CategoryId,
            request.TransactionGroupId,
            request.IncomeSource,
            cancellationToken);
        
        if (result.IsError)
        {
            var error = result.Errors.First();
            if (error.Type == ErrorOr.ErrorType.NotFound)
            {
                return NotFound(new { error = error.Description });
            }
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Delete a transaction
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _transactionService.DeleteAsync(id, cancellationToken);
        
        if (result.IsError)
        {
            var error = result.Errors.First();
            if (error.Type == ErrorOr.ErrorType.NotFound)
            {
                return NotFound(new { error = error.Description });
            }
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }
        
        return NoContent();
    }
}

