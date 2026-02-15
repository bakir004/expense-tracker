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
    /// Get all available categories for expense and income transactions.
    /// </summary>
    /// <remarks>
    /// Retrieves all categories available in the system. Categories are used to classify transactions
    /// for better organization and reporting. The system includes both predefined system categories
    /// and custom categories that users can create.
    ///
    /// **Authentication:** Not required
    ///
    /// **Response Fields:**
    /// - **Id**: Unique identifier for the category
    /// - **Name**: Category name (e.g., "Food", "Transport", "Salary")
    /// - **Description**: Optional detailed description of the category
    /// - **Icon**: Icon identifier for UI representation
    ///
    /// **Use Cases:**
    /// - Populating category dropdowns in transaction forms
    /// - Displaying available categories for filtering
    /// - Category-based transaction reporting
    /// - Budget planning by category
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>List of all available categories with their details</returns>
    /// <response code="200">Categories retrieved successfully - returns array of category objects</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
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
