using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.Incomes;
using SampleCkWebApp.Application.Incomes.Interfaces.Application;
using SampleCkWebApp.Application.Incomes.Mappings;
using SampleCkWebApp.WebApi.Controllers;

namespace SampleCkWebApp.WebApi.Controllers.Incomes;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class IncomesController : ApiControllerBase
{
    private readonly IIncomeService _incomeService;
    
    public IncomesController(IIncomeService incomeService)
    {
        _incomeService = incomeService;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetIncomesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomes(CancellationToken cancellationToken)
    {
        var result = await _incomeService.GetAllAsync(cancellationToken);
        return result.Match(
            incomes => Ok(incomes.ToResponse()),
            Problem);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncomeById([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _incomeService.GetByIdAsync(id, cancellationToken);
        return result.Match(
            income => Ok(income.ToResponse()),
            Problem);
    }
    
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<IncomeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncomesByUserId([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _incomeService.GetByUserIdAsync(userId, cancellationToken);
        return result.Match(
            incomes => Ok(incomes.Select(i => i.ToResponse()).ToList()),
            Problem);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIncome([FromBody, Required] CreateIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await _incomeService.CreateAsync(
            request.Amount,
            request.Description,
            request.Source,
            request.PaymentMethod,
            request.UserId,
            request.Date,
            cancellationToken);
        return result.Match(
            income => CreatedAtAction(nameof(GetIncomeById), new { id = income.Id }, income.ToResponse()),
            Problem);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIncome([FromRoute, Required] int id, [FromBody, Required] UpdateIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await _incomeService.UpdateAsync(
            id,
            request.Amount,
            request.Description,
            request.Source,
            request.PaymentMethod,
            request.Date,
            cancellationToken);
        return result.Match(
            income => Ok(income.ToResponse()),
            Problem);
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncome([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _incomeService.DeleteAsync(id, cancellationToken);
        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

