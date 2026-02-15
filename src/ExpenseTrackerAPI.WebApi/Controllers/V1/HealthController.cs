using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Infrastructure.Persistence;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// Controller for health check operations.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthController : ApiControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the HealthController class.
    /// </summary>
    /// <param name="dbContext">Database context for health checks</param>
    /// <param name="logger">Logger instance</param>
    public HealthController(ApplicationDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the health status of the API.
    /// </summary>
    /// <remarks>
    /// Returns comprehensive health information about the API including:
    ///
    /// - **Status**: Overall health status (Healthy/Unhealthy)
    /// - **Timestamp**: Current UTC timestamp
    /// - **Version**: Application version number
    /// - **Database**: Database connectivity and status
    ///
    /// This endpoint performs the following checks:
    /// 1. Database connection test
    /// 2. Database query execution (user count)
    ///
    /// **Note:** This endpoint does not require authentication and should be publicly accessible for monitoring purposes.
    /// </remarks>
    /// <returns>A health status response containing API and database health information</returns>
    /// <response code="200">API is healthy and all checks passed successfully</response>
    /// <response code="503">API is unhealthy - database connection failed or other critical issues detected</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        var response = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetVersion(),
            Database = await CheckDatabaseHealthAsync()
        };

        if (response.Database.Status != "Healthy")
        {
            response.Status = "Unhealthy";
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        return Ok(response);
    }

    private async Task<DatabaseHealth> CheckDatabaseHealthAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                return new DatabaseHealth
                {
                    Status = "Unhealthy",
                    Message = "Cannot connect to database"
                };
            }

            // Test query to ensure database is responsive
            var userCount = await _dbContext.Users.CountAsync();

            return new DatabaseHealth
            {
                Status = "Healthy",
                Message = "Database connection successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new DatabaseHealth
            {
                Status = "Unhealthy",
                Message = $"Database error: {ex.Message}"
            };
        }
    }

    private static string GetVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }
}

/// <summary>
/// Health check response model.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current UTC timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Database health information.
    /// </summary>
    public DatabaseHealth Database { get; set; } = new();
}

/// <summary>
/// Database health information.
/// </summary>
public class DatabaseHealth
{
    /// <summary>
    /// Database health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Additional health message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
