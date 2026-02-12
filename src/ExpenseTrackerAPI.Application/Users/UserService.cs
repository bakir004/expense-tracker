using ErrorOr;
using ExpenseTrackerAPI.Application.Common.Interfaces;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Users;

/// <summary>
/// Service implementation for user business operations.
/// Handles registration, authentication, and password management using BCrypt and JWT.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
    }

    public async Task<ErrorOr<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidPasswordComplexity(request.Password))
            return UserErrors.WeakPassword;

        var existsResult = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (existsResult.IsError)
            return existsResult.Errors;

        if (existsResult.Value)
            return UserErrors.DuplicateEmail;

        try
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt());

            var user = new User(
                name: request.Name,
                email: request.Email,
                passwordHash: passwordHash,
                initialBalance: request.InitialBalance ?? 0);

            var createResult = await _userRepository.CreateAsync(user, cancellationToken);
            if (createResult.IsError)
                return createResult.Errors;

            var createdUser = createResult.Value;

            return new RegisterResponse(
                Id: createdUser.Id,
                Name: createdUser.Name,
                Email: createdUser.Email,
                InitialBalance: createdUser.InitialBalance,
                CreatedAt: createdUser.CreatedAt);
        }
        catch (ArgumentException)
        {
            return UserErrors.InvalidEmail;
        }
        catch (Exception ex)
        {
            return Error.Failure("User.Register.UnexpectedError", $"Registration failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userResult = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (userResult.IsError)
            {
                // Don't reveal whether email exists or not for security
                return UserErrors.InvalidCredentials;
            }

            var user = userResult.Value;

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return UserErrors.InvalidCredentials;
            }

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Name);
            var expiresAt = DateTime.UtcNow.AddHours(_jwtTokenGenerator.TokenExpirationHours);

            return new LoginResponse(
                Id: user.Id,
                Name: user.Name,
                Email: user.Email,
                InitialBalance: user.InitialBalance,
                Token: token,
                ExpiresAt: expiresAt);
        }
        catch (Exception ex)
        {
            return Error.Failure("User.Login.UnexpectedError", $"Login failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate password complexity requirements.
    /// </summary>
    private static bool HasValidPasswordComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var hasUppercase = password.Any(char.IsUpper);
        var hasLowercase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUppercase && hasLowercase && hasDigit && hasSpecialChar;
    }
}
