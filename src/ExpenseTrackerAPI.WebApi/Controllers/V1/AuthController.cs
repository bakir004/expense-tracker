using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Users;
using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// Controller for user authentication operations.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// </summary>
    /// <param name="userService">User service for authentication operations</param>
    /// <param name="logger">Logger instance</param>
    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <remarks>
    /// Creates a new user account with the provided details. This is the first step for new users to access the ExpenseTracker API.
    ///
    /// **Required Fields:**
    /// - **Name**: User's full name (max 100 characters)
    /// - **Email**: Valid email address (max 254 characters, must be unique)
    /// - **Password**: Strong password (8-100 characters)
    ///   - Must contain at least one uppercase letter
    ///   - Must contain at least one lowercase letter
    ///   - Must contain at least one digit
    ///   - Must contain at least one special character
    ///
    /// **Optional Fields:**
    /// - **InitialBalance**: Starting account balance (defaults to 0.00)
    ///
    /// **Note:** After successful registration, use the `/api/v1/auth/login` endpoint to obtain an authentication token.
    /// </remarks>
    /// <param name="request">User registration details including name, email, password, and optional initial balance</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Registration response containing the newly created user's information</returns>
    /// <response code="201">User successfully registered - returns user details without password</response>
    /// <response code="400">Invalid request data or validation errors (e.g., weak password, invalid email format)</response>
    /// <response code="409">User with the provided email address already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.RegisterAsync(request, cancellationToken);

        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var response = result.Value;
        return Ok(response);
    }

    /// <summary>
    /// Authenticate user and generate access token.
    /// </summary>
    /// <remarks>
    /// Authenticates a user with their email and password, returning a JWT access token for subsequent API requests.
    ///
    /// **Required Fields:**
    /// - **Email**: Registered user's email address
    /// - **Password**: User's password
    ///
    /// **Authentication Flow:**
    /// 1. Submit email and password to this endpoint
    /// 2. Receive JWT token in the response
    /// 3. Include the token in the `Authorization` header for protected endpoints:
    ///    - Header format: `Authorization: Bearer {your-token-here}`
    ///
    /// **Token Details:**
    /// - Token expires after 24 hours (configurable)
    /// - Token contains user ID, email, and name claims
    /// - Token is required for all endpoints except `/auth/register`, `/auth/login`, and health checks
    ///
    /// **Database seeded users have credentials:**
    /// - Email: `john.doe@email.com`, `jane.smith@email.com` and `mike.wilson@email.com`
    /// - Password: `Password123!`
    ///
    /// **Security Notes:**
    /// - Passwords are hashed and never stored in plain text
    /// - Failed login attempts are logged for security monitoring
    /// - Use HTTPS in production to protect credentials in transit
    /// </remarks>
    /// <param name="request">Login credentials containing email and password</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Login response containing user details and JWT access token</returns>
    /// <response code="200">Login successful - returns user details and authentication token</response>
    /// <response code="400">Invalid request data (e.g., missing email or password)</response>
    /// <response code="401">Invalid credentials - email or password is incorrect</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);

        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var response = result.Value;
        return Ok(response);
    }
}
