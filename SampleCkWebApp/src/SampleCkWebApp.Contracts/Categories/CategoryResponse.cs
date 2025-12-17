// ============================================================================
// FILE: CategoryResponse.cs
// ============================================================================
// WHAT: API contract DTO (Data Transfer Object) for category information responses.
//
// WHY: This DTO exists in the Contracts layer to define how category data is
//      returned to API consumers. It's separate from the domain entity (Category)
//      to control what information is exposed and allow for future API versioning.
//
// WHAT IT DOES:
//      - Defines the structure of category data returned by category endpoints
//      - Contains properties: Id, Name, Description, Icon
//      - Includes XML documentation for Swagger/OpenAPI generation
//      - Mapped from Category domain entity using CategoryMappings.ToResponse()
// ============================================================================

namespace SampleCkWebApp.Categories;

/// <summary>
/// Category information response model
/// </summary>
public class CategoryResponse
{
    /// <summary>
    /// Unique identifier for the category
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }
    
    /// <summary>
    /// Category name
    /// </summary>
    /// <example>Food &amp; Dining</example>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Category description
    /// </summary>
    /// <example>Groceries, restaurants, and food delivery</example>
    public string? Description { get; set; }
    
    /// <summary>
    /// Category icon (emoji or icon identifier)
    /// </summary>
    /// <example>🍔</example>
    public string? Icon { get; set; }
}

