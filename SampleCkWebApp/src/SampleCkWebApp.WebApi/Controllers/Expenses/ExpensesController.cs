using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.Expenses;
using SampleCkWebApp.Application.Expenses.Interfaces.Application;
using SampleCkWebApp.Application.Expenses.Mappings;
using SampleCkWebApp.WebApi.Controllers;

namespace SampleCkWebApp.WebApi.Controllers.Expenses;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ExpensesController : ApiControllerBase
{
    private readonly IExpenseService _expenseService;
    
    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetExpensesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenses(CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetAllAsync(cancellationToken);
        return result.Match(
            expenses => Ok(expenses.ToResponse()),
            Problem);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseById([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetByIdAsync(id, cancellationToken);
        return result.Match(
            expense => Ok(expense.ToResponse()),
            Problem);
    }
    
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<ExpenseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpensesByUserId([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetByUserIdAsync(userId, cancellationToken);
        return result.Match(
            expenses => Ok(expenses.Select(e => e.ToResponse()).ToList()),
            Problem);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExpense([FromBody, Required] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.CreateAsync(
            request.Amount,
            request.Date,
            request.Description,
            request.PaymentMethod,
            request.CategoryId,
            request.UserId,
            request.ExpenseGroupId,
            cancellationToken);
        return result.Match(
            expense => CreatedAtAction(nameof(GetExpenseById), new { id = expense.Id }, expense.ToResponse()),
            Problem);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExpense([FromRoute, Required] int id, [FromBody, Required] UpdateExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseService.UpdateAsync(
            id,
            request.Amount,
            request.Date,
            request.Description,
            request.PaymentMethod,
            request.CategoryId,
            request.ExpenseGroupId,
            cancellationToken);
        return result.Match(
            expense => Ok(expense.ToResponse()),
            Problem);
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _expenseService.DeleteAsync(id, cancellationToken);
        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

