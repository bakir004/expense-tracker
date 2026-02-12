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
}
