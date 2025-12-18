using Microsoft.AspNetCore.Mvc;

namespace SampleCkWebApp.WebApi.Controllers.V2;

/// <summary>
/// Test controller demonstrating v2 API versioning
/// </summary>
[ApiController]
[Route($"{ApiRoutes.V2}/test")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Simple test endpoint for v2
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            version = "v2",
            message = "Hello from API v2!",
            timestamp = DateTime.UtcNow
        });
    }
}

