using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SampleCkWebApp.WebApi.Controllers;

/// <summary>
/// Health check endpoint for monitoring application status
/// </summary>
[ApiController]
[Route("health")]
[ApiExplorerSettings(IgnoreApi = false)]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the health status of the application
    /// </summary>
    /// <returns>Health status information</returns>
    /// <response code="200">Application is healthy</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}

