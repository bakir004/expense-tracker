using System.Text.Json;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SampleCkWebApp.WebApi.Controllers;

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
        var statusCode = (int)error.Type switch
        {
            StatusCodes.Status409Conflict => StatusCodes.Status409Conflict,
            (int)ErrorType.Conflict => StatusCodes.Status409Conflict,
            StatusCodes.Status400BadRequest => StatusCodes.Status400BadRequest,
            (int)ErrorType.Validation => StatusCodes.Status400BadRequest,
            StatusCodes.Status404NotFound => StatusCodes.Status404NotFound,
            (int)ErrorType.NotFound => StatusCodes.Status404NotFound,
            StatusCodes.Status401Unauthorized => StatusCodes.Status401Unauthorized,
            (int)ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
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
