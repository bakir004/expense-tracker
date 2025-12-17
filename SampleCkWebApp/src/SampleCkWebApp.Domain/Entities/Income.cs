// ============================================================================
// FILE: Income.cs
// ============================================================================
// WHAT: Domain entity representing income in the expense tracker system.
//
// WHY: This is the core domain model for income. It exists in the Domain layer
//      because it represents the fundamental business concept of income. The
//      Domain layer has no dependencies on other layers, making this a pure
//      representation of the income entity as it exists in the business domain.
//
// WHAT IT DOES:
//      - Defines the structure of an income entity with properties: Id, Amount,
//        Description, Source, PaymentMethod, UserId, and Date
//      - Serves as the source of truth for income data structure across all
//        layers of the application
//      - Used by Application layer for business logic and Infrastructure
//        layer for persistence operations
// ============================================================================

namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents income in the expense tracker system.
/// </summary>
public class Income
{
    public int Id { get; set; }
    
    public decimal Amount { get; set; }
    
    public string? Description { get; set; }
    
    public string? Source { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public int UserId { get; set; }
    
    public DateTime Date { get; set; }
}

