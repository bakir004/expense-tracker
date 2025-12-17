// ============================================================================
// FILE: ICategoryService.cs
// ============================================================================
// WHAT: Interface defining the contract for category application service operations.
//
// WHY: This interface exists in the Application layer to define the public API
//      for category-related business operations. It follows the Interface Segregation
//      Principle by defining only what consumers need. The interface is defined
//      here but implemented in the same layer, allowing for easy testing and
//      potential future implementations (e.g., caching, logging decorators).
//
// WHAT IT DOES:
//      - Defines CRUD operations for categories: GetAllAsync, GetByIdAsync,
//        CreateAsync, UpdateAsync, DeleteAsync
//      - Uses ErrorOr pattern for functional error handling
//      - Returns domain entities (Category) and application DTOs
//      - Acts as the contract that controllers depend on, keeping them
//        decoupled from concrete implementations
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Categories.Data;

namespace SampleCkWebApp.Application.Categories.Interfaces.Application;

/// <summary>
/// Application service interface for category operations.
/// This service orchestrates domain logic and coordinates with the repository.
/// </summary>
public interface ICategoryService
{
    Task<ErrorOr<GetCategoriesResult>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<Category>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<Category>> CreateAsync(string name, string? description, string? icon, CancellationToken cancellationToken);
    
    Task<ErrorOr<Category>> UpdateAsync(int id, string name, string? description, string? icon, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

