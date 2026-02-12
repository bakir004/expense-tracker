// ============================================================================
// FILE: UserRepository.cs
// ============================================================================
// WHAT: Entity Framework Core implementation of the user repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for users. It implements IUserRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      Uses Entity Framework Core for simple CRUD operations and raw SQL
//      for complex queries (like balance calculation with lateral joins).
//
// WHAT IT DOES:
//      - Implements IUserRepository interface with Entity Framework Core
//      - Uses EF Core LINQ for simple operations: get all users, get user by ID,
//        get user by email, create users, and set initial balance
//      - Uses raw SQL for complex balance calculations with lateral joins
//      - Handles database exceptions (unique violations, connection errors)
//      - Returns ErrorOr results for consistent error handling
// ============================================================================

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ExpenseTrackerAPI.Domain.Constants;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Infrastructure.Shared;

namespace ExpenseTrackerAPI.Infrastructure.Users;

/// <summary>
/// Entity Framework Core implementation of the user repository.
/// Uses EF Core for simple CRUD and raw SQL for complex queries.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ExpenseTrackerDbContext _context;

    public UserRepository(ExpenseTrackerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<List<User>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _context.Users
                .OrderBy(u => u.Id)
                .ToListAsync(cancellationToken);

            return users;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve users: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return UserErrors.NotFound;
            }

            return user;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null)
            {
                return UserErrors.NotFound;
            }

            return user;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user by email: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> CreateUserAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresSqlState.UniqueViolation)
        {
            return UserErrors.DuplicateEmail;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<(decimal InitialBalance, decimal CumulativeDelta, decimal CurrentBalance)>> GetUserBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.InitialBalance })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return UserErrors.NotFound;
            }

            var initialBalance = user.InitialBalance;

            var cumulativeDelta = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => t.CumulativeDelta)
                .FirstOrDefaultAsync(cancellationToken);

            var currentBalance = initialBalance + cumulativeDelta;

            return (initialBalance, cumulativeDelta, currentBalance);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user balance: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> SetInitialBalanceAsync(int userId, decimal initialBalance, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return UserErrors.NotFound;
            }

            user.InitialBalance = initialBalance;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to set initial balance: {ex.Message}");
        }
    }
}
