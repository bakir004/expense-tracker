using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.ExpenseGroups;
using SampleCkWebApp.Application.ExpenseGroups.Interfaces.Application;
using SampleCkWebApp.Application.ExpenseGroups.Mappings;
using SampleCkWebApp.WebApi.Controllers;

namespace SampleCkWebApp.WebApi.Controllers.ExpenseGroups;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ExpenseGroupsController : ApiControllerBase
{
    private readonly IExpenseGroupService _expenseGroupService;
    
    public ExpenseGroupsController(IExpenseGroupService expenseGroupService)
    {
        _expenseGroupService = expenseGroupService;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetExpenseGroupsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseGroups(CancellationToken cancellationToken)
    {
        var result = await _expenseGroupService.GetAllAsync(cancellationToken);
        return result.Match(
            groups => Ok(groups.ToResponse()),
            Problem);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpenseGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseGroupById([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _expenseGroupService.GetByIdAsync(id, cancellationToken);
        return result.Match(
            group => Ok(group.ToResponse()),
            Problem);
    }
    
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<ExpenseGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenseGroupsByUserId([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _expenseGroupService.GetByUserIdAsync(userId, cancellationToken);
        return result.Match(
            groups => Ok(groups.Select(g => g.ToResponse()).ToList()),
            Problem);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExpenseGroup([FromBody, Required] CreateExpenseGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseGroupService.CreateAsync(request.Name, request.Description, request.UserId, cancellationToken);
        return result.Match(
            group => CreatedAtAction(nameof(GetExpenseGroupById), new { id = group.Id }, group.ToResponse()),
            Problem);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ExpenseGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExpenseGroup([FromRoute, Required] int id, [FromBody, Required] UpdateExpenseGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _expenseGroupService.UpdateAsync(id, request.Name, request.Description, cancellationToken);
        return result.Match(
            group => Ok(group.ToResponse()),
            Problem);
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpenseGroup([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _expenseGroupService.DeleteAsync(id, cancellationToken);
        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

