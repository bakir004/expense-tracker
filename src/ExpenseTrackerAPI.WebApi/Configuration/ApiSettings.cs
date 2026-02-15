namespace ExpenseTrackerAPI.WebApi.Configuration;

/// <summary>
/// API configuration settings.
/// </summary>
public class ApiSettings
{
    /// <summary>
    /// Maximum number of items allowed per page in paginated responses.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Default number of items per page when not specified.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;
}
