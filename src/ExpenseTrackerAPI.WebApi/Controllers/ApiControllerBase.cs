using System.Text.Json;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ExpenseTrackerAPI.WebApi.Controllers;

/// <summary>
/// Base controller class providing common functionality for API controllers.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Default JSON serializer options for consistent API responses.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Creates a problem response with the specified status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <returns>Problem details response</returns>
    protected IActionResult ProblemCode(int statusCode)
    {
        return Problem(statusCode: statusCode);
    }

    /// <summary>
    /// Creates a problem response from a list of errors, handling validation errors specially.
    /// </summary>
    /// <param name="errors">List of errors to convert to problem response</param>
    /// <returns>Problem details or validation problem response</returns>
    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return Problem();
        }

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        HttpContext.Items[HttpContextItemKeys.Error] = errors;

        return Problem(errors[0]);
    }

    /// <summary>
    /// Creates a problem response from a single error with appropriate HTTP status code.
    /// </summary>
    /// <param name="error">Error to convert to problem response</param>
    /// <returns>Problem details response</returns>
    protected IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status422UnprocessableEntity, // Semantic validation errors
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: error.Description);
    }

    private IActionResult ValidationProblem(List<Error> errors)
    {
        var modelStateDict = new ModelStateDictionary();

        foreach (var error in errors)
        {
            modelStateDict.AddModelError(
                error.Code,
                error.Description);
        }

        return ValidationProblem(modelStateDict);
    }
}
