using System.Text.Json.Serialization;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Errors;

/// <summary>
/// Represents the standard ProblemDetails format (RFC 9110/7807).
/// Used for validating error response structure in API tests.
/// </summary>
public class ProblemDetailsResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Additional extension members (for custom error properties).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; set; }
}

/// <summary>
/// Represents the ValidationProblemDetails format (RFC 9110/7807 with validation errors).
/// Used for validating validation error responses in API tests.
/// </summary>
public class ValidationProblemDetailsResponse : ProblemDetailsResponse
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Helper class for validating error response formats.
/// </summary>
public static class ErrorResponseValidator
{
    /// <summary>
    /// Validates that all error keys in the response do not contain dots.
    /// This ensures proper error key formatting without nested property paths.
    /// </summary>
    /// <param name="errors">Dictionary of error keys and messages</param>
    /// <returns>True if all keys are valid (no dots), false otherwise</returns>
    public static bool AllKeysAreValid(Dictionary<string, string[]>? errors)
    {
        if (errors == null || errors.Count == 0)
            return true;

        return errors.Keys.All(key => !key.Contains('.'));
    }

    /// <summary>
    /// Gets all error keys that contain dots (invalid keys).
    /// </summary>
    /// <param name="errors">Dictionary of error keys and messages</param>
    /// <returns>List of invalid keys containing dots</returns>
    public static List<string> GetInvalidKeys(Dictionary<string, string[]>? errors)
    {
        if (errors == null || errors.Count == 0)
            return new List<string>();

        return errors.Keys.Where(key => key.Contains('.')).ToList();
    }

    /// <summary>
    /// Validates that the response conforms to RFC 9110 ProblemDetails format.
    /// </summary>
    /// <param name="problem">The problem details response to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidProblemDetails(ProblemDetailsResponse? problem)
    {
        if (problem == null)
            return false;

        // Must have a status code
        if (!problem.Status.HasValue)
            return false;

        // Status should be a valid HTTP status code (4xx or 5xx for errors)
        if (problem.Status.Value < 400 || problem.Status.Value >= 600)
            return false;

        // Should have either title or detail
        if (string.IsNullOrWhiteSpace(problem.Title) && string.IsNullOrWhiteSpace(problem.Detail))
            return false;

        return true;
    }

    /// <summary>
    /// Validates that the response conforms to ValidationProblemDetails format.
    /// </summary>
    /// <param name="validation">The validation problem details response to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidValidationProblemDetails(ValidationProblemDetailsResponse? validation)
    {
        if (validation == null)
            return false;

        // Must conform to base ProblemDetails
        if (!IsValidProblemDetails(validation))
            return false;

        // Must have errors dictionary
        if (validation.Errors == null)
            return false;

        // Status should be 400 for validation errors
        if (validation.Status != 400)
            return false;

        // All error keys must not contain dots
        if (!AllKeysAreValid(validation.Errors))
            return false;

        return true;
    }
}
