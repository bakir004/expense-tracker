using System.Security.Claims;

namespace ExpenseTrackerAPI.WebApi.Extensions;

/// <summary>
/// Extension methods for HttpContext to easily access user information.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the authenticated user's ID from HttpContext.Items or JWT claims.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User ID if found and valid, otherwise null</returns>
    public static int? GetUserId(this HttpContext context)
    {
        // Try to get from middleware first (faster)
        if (context.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is int userId)
        {
            return userId;
        }

        // Fallback to reading from claims directly
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
            {
                return parsedUserId;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the authenticated user's ID from HttpContext, throwing an exception if not found.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not found or invalid</exception>
    public static int GetRequiredUserId(this HttpContext context)
    {
        var userId = context.GetUserId();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User ID not found or invalid in authentication context");
        }
        return userId.Value;
    }

    /// <summary>
    /// Gets the authenticated user's email from HttpContext.Items or JWT claims.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User email if found, otherwise null</returns>
    public static string? GetUserEmail(this HttpContext context)
    {
        // Try to get from middleware first
        if (context.Items.TryGetValue("UserEmail", out var emailObj) && emailObj is string email)
        {
            return email;
        }

        // Fallback to reading from claims directly
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        return null;
    }

    /// <summary>
    /// Gets the authenticated user's name from HttpContext.Items or JWT claims.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User name if found, otherwise null</returns>
    public static string? GetUserName(this HttpContext context)
    {
        // Try to get from middleware first
        if (context.Items.TryGetValue("UserName", out var nameObj) && nameObj is string name)
        {
            return name;
        }

        // Fallback to reading from claims directly
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(ClaimTypes.Name)?.Value;
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user has a valid authentication context.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>True if user is authenticated and has a valid user ID</returns>
    public static bool HasValidUserContext(this HttpContext context)
    {
        return context.GetUserId() != null;
    }

    /// <summary>
    /// Checks if the user claim is marked as invalid by the middleware.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>True if the user claim is invalid</returns>
    public static bool HasInvalidUserClaim(this HttpContext context)
    {
        return context.Items.ContainsKey("InvalidUserClaim");
    }
}
