// ============================================================================
// FILE: GetCategoriesResult.cs
// ============================================================================
// WHAT: Application layer data model for category retrieval results.
//
// WHY: This DTO exists in the Application layer to represent the result of
//      retrieving categories. It's separate from the domain entity to allow
//      for future extensibility (pagination, filtering metadata, etc.) without
//      modifying the domain model. It acts as an intermediate representation
//      between the repository (which returns domain entities) and the API
//      layer (which needs API contracts).
//
// WHAT IT DOES:
//      - Contains a list of Category domain entities
//      - Used by CategoryService.GetAllAsync to return categories
//      - Mapped to GetCategoriesResponse (API contract) in CategoryMappings
// ============================================================================

using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Categories.Data;

/// <summary>
/// Result model for retrieving all categories.
/// </summary>
public class GetCategoriesResult
{
    public List<Category> Categories { get; set; } = new();
}

