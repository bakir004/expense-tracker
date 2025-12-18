// ============================================================================
// FILE: CategoryService.cs
// ============================================================================
// WHAT: Application service implementation for category business operations.
//
// WHY: This service exists in the Application layer to orchestrate category-related
//      business logic. It coordinates between domain entities, validation,
//      and persistence. This is where use cases are implemented - it knows
//      HOW to perform category operations by combining validation, domain logic,
//      and data access. It keeps controllers thin by handling all business
//      logic here rather than in the presentation layer.
//
// WHAT IT DOES:
//      - Implements ICategoryService interface with CRUD operations
//      - Validates category input using CategoryValidator before processing
//      - Coordinates with ICategoryRepository for data access
//      - Returns domain entities wrapped in ErrorOr
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Categories.Data;
using SampleCkWebApp.Application.Categories.Interfaces.Application;
using SampleCkWebApp.Application.Categories.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.Categories;

/// <summary>
/// Application service for category operations.
/// Orchestrates domain logic and coordinates with the repository.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }
    
    public async Task<ErrorOr<GetCategoriesResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _categoryRepository.GetAllAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return new GetCategoriesResult
        {
            Categories = result.Value
        };
    }
    
    public async Task<ErrorOr<Category>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return result;
    }
    
    public async Task<ErrorOr<Category>> CreateAsync(string name, string? description, string? icon, CancellationToken cancellationToken)
    {
        var validationResult = CategoryValidator.ValidateCategoryRequest(name, description, icon);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        // Check if category with this name already exists
        var existingCategory = await _categoryRepository.GetByNameAsync(name, cancellationToken);
        if (!existingCategory.IsError)
        {
            return CategoryErrors.DuplicateName;
        }
        
        var category = new Category
        {
            Name = name,
            Description = description,
            Icon = icon
        };
        
        var createResult = await _categoryRepository.CreateAsync(category, cancellationToken);
        return createResult;
    }
    
    public async Task<ErrorOr<Category>> UpdateAsync(int id, string name, string? description, string? icon, CancellationToken cancellationToken)
    {
        var validationResult = CategoryValidator.ValidateCategoryRequest(name, description, icon);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        // Get existing category to ensure it exists
        var existingResult = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }
        
        // Check if another category with this name already exists (excluding current category)
        var duplicateCheck = await _categoryRepository.GetByNameAsync(name, cancellationToken);
        if (!duplicateCheck.IsError && duplicateCheck.Value.Id != id)
        {
            return CategoryErrors.DuplicateName;
        }
        
        var category = new Category
        {
            Id = id,
            Name = name,
            Description = description,
            Icon = icon
        };
        
        var updateResult = await _categoryRepository.UpdateAsync(category, cancellationToken);
        return updateResult;
    }
    
    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _categoryRepository.DeleteAsync(id, cancellationToken);
        return result;
    }
}

