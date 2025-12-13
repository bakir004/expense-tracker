// ============================================================================
// FILE: User.cs
// ============================================================================
// WHAT: Domain entity representing a user in the expense tracker system.
//
// WHY: This is the core domain model for users. It exists in the Domain layer
//      because it represents the fundamental business concept of a user. The
//      Domain layer has no dependencies on other layers, making this a pure
//      representation of the user entity as it exists in the business domain.
//
// WHAT IT DOES:
//      - Defines the structure of a user entity with properties: Id, Name,
//        Email, PasswordHash, CreatedAt, and UpdatedAt
//      - Serves as the source of truth for user data structure across all
//        layers of the application
//      - Used by Application layer for business logic and Infrastructure
//        layer for persistence operations
// ============================================================================

namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents a user in the expense tracker system.
/// </summary>
public class User
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}

