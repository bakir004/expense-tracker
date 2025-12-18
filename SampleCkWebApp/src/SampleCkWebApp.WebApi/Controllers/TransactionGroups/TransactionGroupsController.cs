using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.Contracts.TransactionGroups;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Application;
using SampleCkWebApp.Application.TransactionGroups.Mappings;
using SampleCkWebApp.WebApi.Controllers;

namespace SampleCkWebApp.WebApi.Controllers.TransactionGroups;

[ApiController]
[Route("transaction-groups")]
[Produces("application/json")]
public class TransactionGroupsController : ApiControllerBase
{
    private readonly ITransactionGroupService _transactionGroupService;
    
    public TransactionGroupsController(ITransactionGroupService transactionGroupService)
    {
        _transactionGroupService = transactionGroupService;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(GetTransactionGroupsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionGroups(CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.GetAllAsync(cancellationToken);
        return result.Match(
            groups => Ok(groups.ToResponse()),
            Problem);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionGroupById([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.GetByIdAsync(id, cancellationToken);
        return result.Match(
            group => Ok(group.ToResponse()),
            Problem);
    }
    
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<TransactionGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionGroupsByUserId([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.GetByUserIdAsync(userId, cancellationToken);
        return result.Match(
            groups => Ok(groups.Select(g => g.ToResponse()).ToList()),
            Problem);
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransactionGroup([FromBody, Required] CreateTransactionGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.CreateAsync(request.Name, request.Description, request.UserId, cancellationToken);
        return result.Match(
            group => CreatedAtAction(nameof(GetTransactionGroupById), new { id = group.Id }, group.ToResponse()),
            Problem);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTransactionGroup([FromRoute, Required] int id, [FromBody, Required] UpdateTransactionGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.UpdateAsync(id, request.Name, request.Description, cancellationToken);
        return result.Match(
            group => Ok(group.ToResponse()),
            Problem);
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransactionGroup([FromRoute, Required] int id, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.DeleteAsync(id, cancellationToken);
        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

