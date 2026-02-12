using System.Text.Json;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ExpenseTrackerAPI.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected IActionResult ProblemCode(int statusCode)
    {
        return Problem(statusCode: statusCode);
    }

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
