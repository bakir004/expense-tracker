using ErrorOr;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Infrastructure.Persistence;

namespace ExpenseTrackerAPI.Infrastructure.Users;

/// <summary>
/// EF Core implementation of the user repository.
/// Provides basic CRUD operations for user management.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<List<User>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .ToListAsync(cancellationToken);

            return users;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.GetAll.DatabaseError", $"Failed to retrieve users: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user is null)
                return UserErrors.NotFound;

            return user;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.GetById.DatabaseError", $"Failed to retrieve user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

            if (user is null)
                return UserErrors.NotFound;

            return user;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.GetByEmail.DatabaseError", $"Failed to retrieve user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<bool>> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var exists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

            return exists;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.ExistsByEmail.DatabaseError", $"Failed to check user existence: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> CreateAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            return UserErrors.DuplicateEmail;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.Create.DatabaseError", $"Failed to create user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            return user;
        }
        catch (DbUpdateConcurrencyException)
        {
            // This can happen if the user doesn't exist or was modified by another process
            // Check if user exists to provide appropriate error
            var exists = await _context.Users.AsNoTracking().AnyAsync(u => u.Id == user.Id, cancellationToken);
            return exists ? UserErrors.ConcurrencyError : UserErrors.NotFound;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            return UserErrors.DuplicateEmail;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.Update.DatabaseError", $"Failed to update user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var rowsAffected = await _context.Users
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (rowsAffected == 0)
                return UserErrors.NotFound;

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.Delete.DatabaseError", $"Failed to delete user: {ex.Message}");
        }
    }
}
