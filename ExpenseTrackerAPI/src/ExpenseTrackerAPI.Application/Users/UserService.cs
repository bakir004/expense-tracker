// ============================================================================
// FILE: UserService.cs
// ============================================================================
// WHAT: Application service implementation for user business operations.
//
// WHY: This service exists in the Application layer to orchestrate user-related
//      business logic. It coordinates between domain entities, validation,
//      and persistence. This is where use cases are implemented - it knows
//      HOW to perform user operations by combining validation, domain logic,
//      and data access. It keeps controllers thin by handling all business
//      logic here rather than in the presentation layer.
//
// WHAT IT DOES:
//      - Implements IUserService interface with three operations:
//        GetUsersAsync, GetUserByIdAsync, and CreateUserAsync
//      - Validates user input using UserValidator before processing
//      - Checks for duplicate emails before creating users
//      - Hashes passwords using BCrypt before persistence
//      - Coordinates with IUserRepository for data access
//      - Returns domain entities and application DTOs wrapped in ErrorOr
// ============================================================================

using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Application.Users.Data;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;

namespace ExpenseTrackerAPI.Application.Users;

/// <summary>
/// Application service for user operations.
/// Orchestrates domain logic and coordinates with the repository.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<ErrorOr<GetUsersResult>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var result = await _userRepository.GetUsersAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return new GetUsersResult
        {
            Users = result.Value
        };
    }
    
    public async Task<ErrorOr<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _userRepository.GetUserByIdAsync(id, cancellationToken);
        return result;
    }
    
    public async Task<ErrorOr<User>> CreateUserAsync(string name, string email, string password, CancellationToken cancellationToken)
    {
        var validationResult = UserValidator.ValidateCreateUserRequest(name, email, password);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        // Check if user with email already exists
        var existingUser = await _userRepository.GetUserByEmailAsync(email, cancellationToken);
        if (!existingUser.IsError)
        {
            return UserErrors.DuplicateEmail;
        }
        
        // Hash the password (BCrypt automatically generates salt)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        // Create domain entity
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Persist the user (initial_balance defaults to 0 in the database)
        return await _userRepository.CreateUserAsync(user, cancellationToken);
    }
    
    public async Task<ErrorOr<(decimal InitialBalance, decimal CumulativeDelta, decimal CurrentBalance)>> GetUserBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        return await _userRepository.GetUserBalanceAsync(userId, cancellationToken);
    }
    
    public async Task<ErrorOr<User>> SetInitialBalanceAsync(int userId, decimal initialBalance, CancellationToken cancellationToken)
    {
        return await _userRepository.SetInitialBalanceAsync(userId, initialBalance, cancellationToken);
    }
}

