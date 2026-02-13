using Microsoft.AspNetCore.Mvc;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Application;
using ExpenseTrackerAPI.Contracts.Categories;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Controllers.V1;

/// <summary>
/// Category management endpoints for API version 1.0.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/categories")]
[ApiVersion("1.0")]
[Authorize]
public class CategoryController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the CategoryController class.
    /// </summary>
    /// <param name="categoryService">Category service</param>
    /// <param name="logger">Logger instance</param>
    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    /// <response code="200">Categories retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var unauthorizedResult = CheckUserContext();
        if (unauthorizedResult != null) return unauthorizedResult;

        var result = await _categoryService.GetAllAsync(cancellationToken);

        if (result.IsError)
        {
            _logger.LogWarning("Failed to get categories: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return Problem(result.Errors);
        }

        var categories = result.Value;
        return Ok(categories.ToResponses());
    }
}
