// ============================================================================
// FILE: UserBalance.cs
// ============================================================================
// WHAT: Domain entity representing a user's balance in the expense tracker system.
//
// WHY: This is the core domain model for user balance. It exists in the Domain layer
//      because it represents the fundamental business concept of a user's current
//      financial balance. The Domain layer has no dependencies on other layers,
//      making this a pure representation of the balance entity as it exists in
//      the business domain.
//
// WHAT IT DOES:
//      - Defines the structure of a user balance entity with properties: Id,
//        UserId, CurrentBalance, InitialBalance, and LastUpdated
//      - Serves as the source of truth for balance data structure across all
//        layers of the application
//      - Used by Application layer for business logic and Infrastructure
//        layer for persistence operations
// ============================================================================

namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents a user's balance in the expense tracker system.
/// </summary>
public class UserBalance
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public decimal CurrentBalance { get; set; }
    
    public decimal InitialBalance { get; set; }
    
    public DateTime LastUpdated { get; set; }
}

