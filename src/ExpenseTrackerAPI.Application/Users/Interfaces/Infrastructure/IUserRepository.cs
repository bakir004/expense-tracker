using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for user persistence operations.
/// Defined in Application layer, implemented in Infrastructure layer.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get all users (admin use)
    /// </summary>
    Task<ErrorOr<List<User>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get a single user by ID
    /// </summary>
    Task<ErrorOr<User>> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Get a user by email address (for login/authentication)
    /// </summary>
    Task<ErrorOr<User>> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Check if a user exists with the given email address
    /// </summary>
    Task<ErrorOr<bool>> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Create a new user
    /// </summary>
    Task<ErrorOr<User>> CreateAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Update an existing user
    /// </summary>
    Task<ErrorOr<User>> UpdateAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a user by ID
    /// </summary>
    Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken);
}
