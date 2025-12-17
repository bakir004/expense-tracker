// ============================================================================
// FILE: IUserBalanceRepository.cs
// ============================================================================
// WHAT: Interface defining the contract for user balance data persistence operations.
//
// WHY: This interface exists in the Application layer (under Interfaces/Infrastructure)
//      to follow the Dependency Inversion Principle. The Application layer defines
//      what it needs from persistence, and the Infrastructure layer implements it.
//      This allows the Application layer to remain independent of database-specific
//      implementations, making it testable and allowing database technology to be
//      swapped without changing business logic.
//
// WHAT IT DOES:
//      - Defines methods for user balance persistence: GetByUserIdAsync, CreateAsync,
//        UpdateBalanceAsync, and RecalculateBalanceAsync
//      - Returns domain entities (UserBalance) not database-specific types
//      - Uses ErrorOr pattern for consistent error handling
//      - Implemented by UserBalanceRepository in Infrastructure layer using PostgreSQL
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.UserBalances.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for user balance persistence operations.
/// This interface is defined in the Application layer but implemented in the Infrastructure layer.
/// </summary>
public interface IUserBalanceRepository
{
    Task<ErrorOr<UserBalance>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserBalance>> CreateAsync(UserBalance userBalance, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserBalance>> UpdateBalanceAsync(int userId, decimal amountChange, DateTime transactionDate, string transactionType, int? transactionId, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserBalance>> RecalculateBalanceAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<decimal>> CalculateBalanceAtDateAsync(int userId, DateTime targetDate, CancellationToken cancellationToken);
}

