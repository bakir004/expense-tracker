// ============================================================================
// FILE: ExpenseGroup.cs
// ============================================================================
// WHAT: Domain entity representing an expense group in the expense tracker system.
//
// WHY: This is the core domain model for expense groups. It exists in the Domain layer
//      because it represents the fundamental business concept of an expense group. The
//      Domain layer has no dependencies on other layers, making this a pure
//      representation of the expense group entity as it exists in the business domain.
//
// WHAT IT DOES:
//      - Defines the structure of an expense group entity with properties: Id, Name,
//        Description, and UserId
//      - Serves as the source of truth for expense group data structure across all
//        layers of the application
//      - Used by Application layer for business logic and Infrastructure
//        layer for persistence operations
// ============================================================================

namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents an expense group in the expense tracker system.
/// </summary>
public class ExpenseGroup
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int UserId { get; set; }
}

