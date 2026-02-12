using System.Security.Claims;

namespace ExpenseTrackerAPI.WebApi.Middleware;

/// <summary>
/// Middleware to extract and validate user ID from JWT claims and add it to HttpContext.
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the UserContextMiddleware class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to extract user context from JWT claims.
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only process authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                // Store user ID in HttpContext.Items for easy access in controllers
                context.Items["UserId"] = userId;

                // Also store user email and name if available
                var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var nameClaim = context.User.FindFirst(ClaimTypes.Name)?.Value;

                if (!string.IsNullOrEmpty(emailClaim))
                    context.Items["UserEmail"] = emailClaim;

                if (!string.IsNullOrEmpty(nameClaim))
                    context.Items["UserName"] = nameClaim;

                _logger.LogDebug("User context set for UserId: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid or missing user ID claim in JWT token");
                context.Items["InvalidUserClaim"] = true;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering the UserContextMiddleware.
/// </summary>
public static class UserContextMiddlewareExtensions
{
    /// <summary>
    /// Adds the UserContextMiddleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
