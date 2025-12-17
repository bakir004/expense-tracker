// ============================================================================
// FILE: IIncomeRepository.cs
// ============================================================================
// WHAT: Interface defining the contract for income data persistence operations.
//
// WHY: This interface exists in the Application layer (under Interfaces/Infrastructure)
//      to follow the Dependency Inversion Principle. The Application layer defines
//      what it needs from persistence, and the Infrastructure layer implements it.
//      This allows the Application layer to remain independent of database-specific
//      implementations, making it testable and allowing database technology to be
//      swapped without changing business logic.
//
// WHAT IT DOES:
//      - Defines methods for income persistence: GetAllAsync, GetByIdAsync,
//        GetByUserIdAsync, CreateAsync, UpdateAsync, and DeleteAsync
//      - Returns domain entities (Income) not database-specific types
//      - Uses ErrorOr pattern for consistent error handling
//      - Implemented by IncomeRepository in Infrastructure layer using PostgreSQL
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.Incomes.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for income persistence operations.
/// This interface is defined in the Application layer but implemented in the Infrastructure layer.
/// </summary>
public interface IIncomeRepository
{
    Task<ErrorOr<List<Income>>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<Income>> GetByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<List<Income>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<Income>> CreateAsync(Income income, CancellationToken cancellationToken);
    
    Task<ErrorOr<Income>> UpdateAsync(Income income, CancellationToken cancellationToken);
    
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}

