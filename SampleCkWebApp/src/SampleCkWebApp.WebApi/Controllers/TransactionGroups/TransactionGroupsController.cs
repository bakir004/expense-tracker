using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.Contracts.TransactionGroups;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Application;
using SampleCkWebApp.Application.TransactionGroups.Mappings;
using SampleCkWebApp.WebApi.Controllers;
using SampleCkWebApp.WebApi;

namespace SampleCkWebApp.WebApi.Controllers.TransactionGroups;

[ApiController]
[Route(ApiRoutes.V1Routes.TransactionGroups)]
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
    
    /// <summary>
    /// Gets all transaction groups for a specific user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transaction groups for the user (empty array if user has no groups)</returns>
    /// <response code="200">Successfully retrieved transaction groups (may be empty)</response>
    /// <response code="404">User not found</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<TransactionGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionGroupsByUserId([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _transactionGroupService.GetByUserIdAsync(userId, cancellationToken);
        return result.Match(
            groups => Ok(groups.Select(g => g.ToResponse()).ToList()),
            Problem);
    }
    
    /// <summary>
    /// Creates a new transaction group
    /// </summary>
    /// <param name="request">Transaction group creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created transaction group</returns>
    /// <response code="201">Transaction group created successfully</response>
    /// <response code="400">Validation error (invalid name or userId)</response>
    /// <response code="422">User does not exist</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

