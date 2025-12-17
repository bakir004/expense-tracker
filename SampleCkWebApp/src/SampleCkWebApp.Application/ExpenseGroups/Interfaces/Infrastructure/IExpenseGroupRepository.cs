// ============================================================================
// FILE: IExpenseGroupRepository.cs
// ============================================================================
// WHAT: Interface defining the contract for expense group data persistence operations.
//
// WHY: This interface exists in the Application layer (under Interfaces/Infrastructure)
//      to follow the Dependency Inversion Principle. The Application layer defines
//      what it needs from persistence, and the Infrastructure layer implements it.
//      This allows the Application layer to remain independent of database-specific
//      implementations, making it testable and allowing database technology to be
//      swapped without changing business logic.
//
// WHAT IT DOES:
//      - Defines methods for expense group persistence: GetAllAsync, GetByIdAsync,
//        GetByUserIdAsync, CreateAsync, UpdateAsync, and DeleteAsync
//      - Returns domain entities (ExpenseGroup) not database-specific types
//      - Uses ErrorOr pattern for consistent error handling
//      - Implemented by ExpenseGroupRepository in Infrastructure layer using PostgreSQL
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.ExpenseGroups.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for expense group persistence operations.
/// This interface is defined in the Application layer but implemented in the Infrastructure layer.
/// </summary>
public interface IExpenseGroupRepository
{
    Task<ErrorOr<List<ExpenseGroup>>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<ExpenseGroup>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<ExpenseGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<ExpenseGroup>> CreateAsync(ExpenseGroup expenseGroup, CancellationToken cancellationToken);
    
    Task<ErrorOr<ExpenseGroup>> UpdateAsync(ExpenseGroup expenseGroup, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

