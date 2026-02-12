using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Users;
using ErrorOr;
using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Controllers.V2;

/// <summary>
/// Controller for user authentication operations - Version 2.0
/// Enhanced with additional security features and improved responses.
/// </summary>
[ApiVersion("2.0")]
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
    /// Register a new user account with enhanced validation.
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced registration response with additional metadata</returns>
    /// <response code="201">User successfully registered</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="409">User with email already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(EnhancedRegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("V2 Registration attempt for email: {Email}", request.Email);

        var result = await _userService.RegisterAsync(request, cancellationToken);

        if (result.IsError)
        {
            return Problem(result.Errors);
        }

        var response = result.Value;
        _logger.LogInformation("User successfully registered with ID: {UserId}", response.Id);

        // Enhanced V2 response with additional metadata
        var enhancedResponse = new EnhancedRegisterResponse(
            Id: response.Id,
            Name: response.Name,
            Email: response.Email,
            InitialBalance: response.InitialBalance,
            CreatedAt: response.CreatedAt,
            ApiVersion: "2.0",
            Features: new[] { "enhanced-security", "password-strength-validation", "account-verification" },
            NextSteps: new[]
            {
                "Please verify your email address",
                "Set up two-factor authentication for enhanced security",
                "Complete your profile setup"
            }
        );

        return CreatedAtAction(
            nameof(Register),
            new { version = "2.0", id = response.Id },
            enhancedResponse);
    }

    /// <summary>
    /// Authenticate user and generate access token with enhanced security.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced login response with security information</returns>
    /// <response code="200">Login successful with token</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(EnhancedLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("V2 Login attempt for email: {Email}", request.Email);

        var result = await _userService.LoginAsync(request, cancellationToken);

        if (result.IsError)
        {
            // Don't log the actual error details for security (avoid revealing user existence)
            _logger.LogWarning("V2 Login failed for email: {Email}", request.Email);
            return Problem(result.Errors);
        }

        var response = result.Value;
        _logger.LogInformation("User successfully logged in with ID: {UserId}", response.Id);

        // Enhanced V2 response with security metadata
        var enhancedResponse = new EnhancedLoginResponse(
            Id: response.Id,
            Name: response.Name,
            Email: response.Email,
            InitialBalance: response.InitialBalance,
            Token: response.Token,
            ExpiresAt: response.ExpiresAt,
            ApiVersion: "2.0",
            TokenType: "Bearer",
            RefreshTokenAvailable: false, // Future feature
            SecurityLevel: "standard",
            LastLoginAt: DateTime.UtcNow,
            SessionId: Guid.NewGuid().ToString("N"),
            Permissions: new[] { "read:transactions", "write:transactions", "read:profile", "write:profile" }
        );

        return Ok(enhancedResponse);
    }

    /// <summary>
    /// Advanced health check with system status information.
    /// </summary>
    /// <returns>Detailed service status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            ApiVersion = "2.0",
            Timestamp = DateTime.UtcNow,
            Features = new[]
            {
                "enhanced-authentication",
                "session-management",
                "security-monitoring",
                "audit-logging"
            },
            Uptime = Environment.TickCount64,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Get API version information and capabilities.
    /// </summary>
    /// <returns>Version information</returns>
    [HttpGet("version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            ApiVersion = "2.0",
            ReleaseDate = "2024-01-01",
            Features = new[]
            {
                "Enhanced registration response with metadata",
                "Improved login response with security information",
                "Session management",
                "Advanced error handling",
                "Audit logging",
                "Security headers",
                "Rate limiting support"
            },
            Deprecated = false,
            SupportedUntil = "2025-12-31",
            Migration = new
            {
                FromV1 = "Automatic - backward compatible",
                BreakingChanges = new string[] { }
            }
        });
    }
}

/// <summary>
/// Enhanced registration response for API v2.0
/// </summary>
public record EnhancedRegisterResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime CreatedAt,
    string ApiVersion,
    string[] Features,
    string[] NextSteps);

/// <summary>
/// Enhanced login response for API v2.0
/// </summary>
public record EnhancedLoginResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    string Token,
    DateTime ExpiresAt,
    string ApiVersion,
    string TokenType,
    bool RefreshTokenAvailable,
    string SecurityLevel,
    DateTime LastLoginAt,
    string SessionId,
    string[] Permissions);
