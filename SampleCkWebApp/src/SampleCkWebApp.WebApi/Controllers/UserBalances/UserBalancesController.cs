using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.UserBalances;
using SampleCkWebApp.Application.UserBalances.Interfaces.Application;
using SampleCkWebApp.Application.UserBalances.Mappings;
using SampleCkWebApp.WebApi.Controllers;

namespace SampleCkWebApp.WebApi.Controllers.UserBalances;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class UserBalancesController : ApiControllerBase
{
    private readonly IUserBalanceService _userBalanceService;
    
    public UserBalancesController(IUserBalanceService userBalanceService)
    {
        _userBalanceService = userBalanceService;
    }
    
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(UserBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserBalance([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _userBalanceService.GetByUserIdAsync(userId, cancellationToken);
        return result.Match(
            balance => Ok(balance.ToResponse()),
            Problem);
    }
    
    [HttpPost("initialize")]
    [ProducesResponseType(typeof(UserBalanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitializeUserBalance([FromBody, Required] InitializeUserBalanceRequest request, CancellationToken cancellationToken)
    {
        var result = await _userBalanceService.InitializeBalanceAsync(request.UserId, request.InitialBalance, cancellationToken);
        return result.Match(
            balance => CreatedAtAction(nameof(GetUserBalance), new { userId = balance.UserId }, balance.ToResponse()),
            Problem);
    }
    
    [HttpPost("user/{userId}/recalculate")]
    [ProducesResponseType(typeof(UserBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecalculateBalance([FromRoute, Required] int userId, CancellationToken cancellationToken)
    {
        var result = await _userBalanceService.RecalculateBalanceAsync(userId, cancellationToken);
        return result.Match(
            balance => Ok(balance.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Gets the user's balance at a specific date using cumulative calculation
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="targetDate">The date to calculate the balance for (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The calculated balance at the target date</returns>
    /// <response code="200">Balance calculated successfully</response>
    /// <response code="404">User balance not found</response>
    /// <response code="400">Invalid user ID or date</response>
    [HttpGet("user/{userId}/balance-at-date")]
    [ProducesResponseType(typeof(BalanceAtDateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBalanceAtDate(
        [FromRoute, Required] int userId,
        [FromQuery, Required] DateTime targetDate,
        CancellationToken cancellationToken)
    {
        var result = await _userBalanceService.GetBalanceAtDateAsync(userId, targetDate, cancellationToken);
        return result.Match(
            balance => Ok(new BalanceAtDateResponse
            {
                UserId = userId,
                TargetDate = targetDate,
                Balance = balance
            }),
            Problem);
    }
}

