// ============================================================================
// FILE: IUserBalanceService.cs
// ============================================================================
// WHAT: Interface defining the contract for user balance application services.
//
// WHY: This interface exists in the Application layer to define the business
//      operations available for user balances. It separates the service contract
//      from its implementation, allowing for easier testing and future modifications.
//      Controllers depend on this interface, not the concrete implementation.
//
// WHAT IT DOES:
//      - Defines methods for user balance operations: GetByUserIdAsync,
//        InitializeBalanceAsync, and RecalculateBalanceAsync
//      - Returns domain entities wrapped in ErrorOr for consistent error handling
//      - Implemented by UserBalanceService
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.UserBalances.Interfaces.Application;

/// <summary>
/// Application service interface for user balance operations.
/// </summary>
public interface IUserBalanceService
{
    Task<ErrorOr<UserBalance>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserBalance>> InitializeBalanceAsync(int userId, decimal initialBalance, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserBalance>> RecalculateBalanceAsync(int userId, CancellationToken cancellationToken);
    
    Task<ErrorOr<decimal>> GetBalanceAtDateAsync(int userId, DateTime targetDate, CancellationToken cancellationToken);
}

