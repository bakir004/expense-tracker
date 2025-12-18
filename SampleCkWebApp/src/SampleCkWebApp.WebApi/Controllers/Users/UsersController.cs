// ============================================================================
// FILE: UsersController.cs
// ============================================================================
// WHAT: ASP.NET Core API controller for user management endpoints.
//
// WHY: This controller exists in the WebApi (presentation) layer to handle
//      HTTP requests and responses for user operations. It's the entry point
//      for the API and is responsible only for HTTP concerns (routing, status
//      codes, request/response formatting). All business logic is delegated
//      to the Application layer (UserService), keeping this controller thin
//      and focused on presentation concerns.
//
// WHAT IT DOES:
//      - Exposes three REST endpoints:
//        * GET /users - Retrieves all users
//        * GET /users/{id} - Retrieves a specific user by ID
//        * POST /users - Creates a new user
//      - Delegates business logic to IUserService
//      - Maps service results to API responses using UserMappings
//      - Handles HTTP status codes and error responses
//      - Includes Swagger/OpenAPI documentation attributes
//      - Uses ErrorOr pattern for functional error handling
// ============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.Users;
using SampleCkWebApp.Application.Users.Interfaces.Application;
using SampleCkWebApp.Application.Users.Mappings;
using SampleCkWebApp.WebApi.Controllers;
using SampleCkWebApp.WebApi;

namespace SampleCkWebApp.WebApi.Controllers.Users;

/// <summary>
/// Controller for managing users in the expense tracker system
/// </summary>
[ApiController]
[Route(ApiRoutes.V1Routes.Users)]
[Produces("application/json")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    /// <summary>
    /// Retrieves all users from the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all users</returns>
    /// <response code="200">Successfully retrieved users</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(cancellationToken);
        
        return result.Match(
            users => Ok(users.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Retrieves a specific user by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user information</returns>
    /// <response code="200">User found and returned</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserById(
        [FromRoute, Required] int id, 
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        
        return result.Match(
            user => Ok(user.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Creates a new user in the system
    /// </summary>
    /// <param name="request">User creation request containing name, email, and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created user</returns>
    /// <response code="201">User successfully created</response>
    /// <response code="400">Validation error (invalid name, email, or password)</response>
    /// <response code="409">User with this email already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser(
        [FromBody, Required] CreateUserRequest request, 
        CancellationToken cancellationToken)
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
    
    /// <summary>
    /// Gets the current balance for a user
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's balance information including initial balance, cumulative delta, and current balance</returns>
    /// <response code="200">Balance retrieved successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/balance")]
    [ProducesResponseType(typeof(UserBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserBalance(
        [FromRoute, Required] int id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserBalanceAsync(id, cancellationToken);
        
        return result.Match(
            balance => Ok(new UserBalanceResponse
            {
                UserId = id,
                InitialBalance = balance.InitialBalance,
                CumulativeDelta = balance.CumulativeDelta,
                CurrentBalance = balance.CurrentBalance
            }),
            Problem);
    }
    
    /// <summary>
    /// Sets the initial balance for a user
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <param name="request">Request containing the initial balance to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated user</returns>
    /// <response code="200">Initial balance updated successfully</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}/initial-balance")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetInitialBalance(
        [FromRoute, Required] int id,
        [FromBody, Required] SetInitialBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.SetInitialBalanceAsync(id, request.InitialBalance, cancellationToken);
        
        return result.Match(
            user => Ok(user.ToResponse()),
            Problem);
    }
}

