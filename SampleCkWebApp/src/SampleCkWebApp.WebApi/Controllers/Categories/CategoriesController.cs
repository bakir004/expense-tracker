// ============================================================================
// FILE: CategoriesController.cs
// ============================================================================
// WHAT: ASP.NET Core API controller for category management endpoints.
//
// WHY: This controller exists in the WebApi (presentation) layer to handle
//      HTTP requests and responses for category operations. It's the entry point
//      for the API and is responsible only for HTTP concerns (routing, status
//      codes, request/response formatting). All business logic is delegated
//      to the Application layer (CategoryService), keeping this controller thin
//      and focused on presentation concerns.
//
// WHAT IT DOES:
//      - Exposes CRUD REST endpoints for categories
//      - Delegates business logic to ICategoryService
//      - Maps service results to API responses using CategoryMappings
//      - Handles HTTP status codes and error responses
//      - Includes Swagger/OpenAPI documentation attributes
//      - Uses ErrorOr pattern for functional error handling
// ============================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using SampleCkWebApp.Categories;
using SampleCkWebApp.Application.Categories.Interfaces.Application;
using SampleCkWebApp.Application.Categories.Mappings;
using SampleCkWebApp.WebApi.Controllers;
using SampleCkWebApp.WebApi;

namespace SampleCkWebApp.WebApi.Controllers.Categories;

/// <summary>
/// Controller for managing categories in the expense tracker system
/// </summary>
[ApiController]
[Route(ApiRoutes.V1Routes.Categories)]
[Produces("application/json")]
public class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;
    
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }
    
    /// <summary>
    /// Retrieves all categories from the system
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    /// <response code="200">Successfully retrieved categories</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetCategoriesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(cancellationToken);
        
        return result.Match(
            categories => Ok(categories.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Retrieves a specific category by its ID
    /// </summary>
    /// <param name="id">The unique identifier of the category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The category information</returns>
    /// <response code="200">Category found and returned</response>
    /// <response code="404">Category not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(
        [FromRoute, Required] int id, 
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);
        
        return result.Match(
            category => Ok(category.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Creates a new category in the system
    /// </summary>
    /// <param name="request">Category creation request containing name, description, and icon</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created category</returns>
    /// <response code="201">Category successfully created</response>
    /// <response code="400">Validation error</response>
    /// <response code="409">Category with this name already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody, Required] CreateCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(
            request.Name,
            request.Description,
            request.Icon,
            cancellationToken);
        
        return result.Match(
            category => CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Updates an existing category
    /// </summary>
    /// <param name="id">The unique identifier of the category</param>
    /// <param name="request">Category update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated category</returns>
    /// <response code="200">Category successfully updated</response>
    /// <response code="400">Validation error</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">Category with this name already exists</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute, Required] int id,
        [FromBody, Required] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(
            id,
            request.Name,
            request.Description,
            request.Icon,
            cancellationToken);
        
        return result.Match(
            category => Ok(category.ToResponse()),
            Problem);
    }
    
    /// <summary>
    /// Deletes a category from the system
    /// </summary>
    /// <param name="id">The unique identifier of the category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Category successfully deleted</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">Cannot delete category because it is referenced by expenses</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        [FromRoute, Required] int id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(id, cancellationToken);
        
        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

