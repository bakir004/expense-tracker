// ============================================================================
// FILE: IUserRepository.cs
// ============================================================================
// WHAT: Interface defining the contract for user data persistence operations.
//
// WHY: This interface exists in the Application layer (under Interfaces/Infrastructure)
//      to follow the Dependency Inversion Principle. The Application layer defines
//      what it needs from persistence, and the Infrastructure layer implements it.
//      This allows the Application layer to remain independent of database-specific
//      implementations, making it testable and allowing database technology to be
//      swapped without changing business logic.
//
// WHAT IT DOES:
//      - Defines methods for user persistence: GetUsersAsync, GetUserByIdAsync,
//        GetUserByEmailAsync, and CreateUserAsync
//      - Returns domain entities (User) not database-specific types
//      - Uses ErrorOr pattern for consistent error handling
//      - Implemented by UserRepository in Infrastructure layer using PostgreSQL
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.Users.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for user persistence operations.
/// This interface is defined in the Application layer but implemented in the Infrastructure layer.
/// </summary>
public interface IUserRepository
{
    Task<ErrorOr<List<User>>> GetUsersAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    
    Task<ErrorOr<User>> CreateUserAsync(User user, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets user balance by combining initial_balance with the cumulative_delta from latest transaction
    /// </summary>
    Task<ErrorOr<(decimal InitialBalance, decimal CumulativeDelta, decimal CurrentBalance)>> GetUserBalanceAsync(int userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Sets the initial balance for a user
    /// </summary>
    Task<ErrorOr<User>> SetInitialBalanceAsync(int userId, decimal initialBalance, CancellationToken cancellationToken);
}

