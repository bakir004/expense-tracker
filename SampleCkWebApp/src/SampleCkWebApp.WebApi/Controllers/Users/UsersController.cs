using Microsoft.AspNetCore.Mvc;
using SampleCkWebApp.Application.Users.Interfaces.Application;
using SampleCkWebApp.Users;
using SampleCkWebApp.WebApi.Controllers;
using SampleCkWebApp.WebApi.Mappings;

namespace SampleCkWebApp.WebApi.Controllers.Users;

[ApiController]
[Route("users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(cancellationToken);
        
        return result.Match(
            users => Ok(users.ToResponse()),
            Problem);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        
        return result.Match(
            user => Ok(user.ToResponse()),
            Problem);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(
            request.Name,
            request.Email,
            request.Password,
            cancellationToken);
        
        return result.Match(
            user => CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user.ToResponse()),
            Problem);
    }
}

