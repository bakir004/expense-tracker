using ErrorOr;
using ExpenseTrackerAPI.Contracts.Users;

namespace ExpenseTrackerAPI.Application.Users.Interfaces.Application;

/// <summary>
/// Service interface for user business operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration response or error</returns>
    Task<ErrorOr<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticate a user and generate access token.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with token or error</returns>
    Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Update user profile information including name, email, password, and initial balance.
    /// </summary>
    /// <param name="userId">ID of the user to update</param>
    /// <param name="request">Update details including current password for verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user response or error</returns>
    Task<ErrorOr<UpdateUserResponse>> UpdateAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Delete user account (hard delete).
    /// </summary>
    /// <param name="userId">ID of the user to delete</param>
    /// <param name="request">Delete request with password verification and confirmation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delete confirmation response or error</returns>
    Task<ErrorOr<DeleteUserResponse>> DeleteAsync(int userId, DeleteUserRequest request, CancellationToken cancellationToken);
}
