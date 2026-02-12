// ============================================================================
// FILE: Category.cs
// ============================================================================
// WHAT: Domain entity representing a category in the expense tracker system.
//
// WHY: This is the core domain model for categories. It exists in the Domain layer
//      because it represents the fundamental business concept of a category. The
//      Domain layer has no dependencies on other layers, making this a pure
//      representation of the category entity as it exists in the business domain.
//
// WHAT IT DOES:
//      - Defines the structure of a category entity with properties: Id, Name,
//        Description, and Icon
//      - Serves as the source of truth for category data structure across all
//        layers of the application
//      - Used by Application layer for business logic and Infrastructure
//        layer for persistence operations
// ============================================================================

namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents a category in the expense tracker system.
/// </summary>
public class Category
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? Icon { get; set; }
}

