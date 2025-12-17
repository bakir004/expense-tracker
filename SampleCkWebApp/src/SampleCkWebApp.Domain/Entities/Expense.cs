// ============================================================================
// FILE: Expense.cs
// ============================================================================
// WHAT: Domain entity representing an expense in the expense tracker system.
//
// WHY: This is the core domain model for expenses. It exists in the Domain layer
//      because it represents the fundamental business concept of an expense. The
//      Domain layer has no dependencies on other layers, making this a pure
//      representation of the expense entity as it exists in the business domain.
//
// WHAT IT DOES:
//      - Defines the structure of an expense entity with properties: Id, Amount,
//        Date, Description, PaymentMethod, CategoryId, UserId, and ExpenseGroupId
//      - Serves as the source of truth for expense data structure across all
//        layers of the application
//      - Used by Application layer for business logic and Infrastructure
//        layer for persistence operations
// ============================================================================

namespace SampleCkWebApp.Domain.Entities;

/// <summary>
/// Represents an expense in the expense tracker system.
/// </summary>
public class Expense
{
    public int Id { get; set; }
    
    public decimal Amount { get; set; }
    
    public DateTime Date { get; set; }
    
    public string? Description { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public int CategoryId { get; set; }
    
    public int UserId { get; set; }
    
    public int? ExpenseGroupId { get; set; }
}

