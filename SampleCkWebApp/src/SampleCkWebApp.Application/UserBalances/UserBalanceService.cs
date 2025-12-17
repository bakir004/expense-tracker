// ============================================================================
// FILE: UserBalanceService.cs
// ============================================================================
// WHAT: Application service implementation for user balance operations.
//
// WHY: This service exists in the Application layer to orchestrate user balance-related
//      business logic. It coordinates between domain entities, validation,
//      and persistence. This is where use cases are implemented - it knows
//      HOW to perform balance operations by combining validation, domain logic,
//      and data access. It keeps controllers thin by handling all business
//      logic here rather than in the presentation layer.
//
// WHAT IT DOES:
//      - Implements IUserBalanceService interface with operations:
//        GetByUserIdAsync, InitializeBalanceAsync, and RecalculateBalanceAsync
//      - Validates balance operations
//      - Coordinates with IUserBalanceRepository for data access
//      - Returns domain entities wrapped in ErrorOr
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.UserBalances.Interfaces.Application;
using SampleCkWebApp.Application.UserBalances.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.UserBalances;

/// <summary>
/// Application service for user balance operations.
/// Orchestrates domain logic and coordinates with the repository.
/// </summary>
public class UserBalanceService : IUserBalanceService
{
    private readonly IUserBalanceRepository _userBalanceRepository;

    public UserBalanceService(IUserBalanceRepository userBalanceRepository)
    {
        _userBalanceRepository = userBalanceRepository ?? throw new ArgumentNullException(nameof(userBalanceRepository));
    }
    
    public async Task<ErrorOr<UserBalance>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var result = await _userBalanceRepository.GetByUserIdAsync(userId, cancellationToken);
        return result;
    }
    
    public async Task<ErrorOr<UserBalance>> InitializeBalanceAsync(int userId, decimal initialBalance, CancellationToken cancellationToken)
    {
        var userBalance = new UserBalance
        {
            UserId = userId,
            CurrentBalance = initialBalance,
            InitialBalance = initialBalance,
            LastUpdated = DateTime.UtcNow
        };
        
        var result = await _userBalanceRepository.CreateAsync(userBalance, cancellationToken);
        return result;
    }
    
    public async Task<ErrorOr<UserBalance>> RecalculateBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        var result = await _userBalanceRepository.RecalculateBalanceAsync(userId, cancellationToken);
        return result;
    }
    
    public async Task<ErrorOr<decimal>> GetBalanceAtDateAsync(int userId, DateTime targetDate, CancellationToken cancellationToken)
    {
        if (userId <= 0) return UserBalanceErrors.InvalidUserId;
        
        var result = await _userBalanceRepository.CalculateBalanceAtDateAsync(userId, targetDate, cancellationToken);
        return result;
    }
}

