using ErrorOr;
using SampleCkWebApp.Application.Users.Data;
using SampleCkWebApp.Application.Users.Interfaces.Application;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.Users;

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
    
    public async Task<ErrorOr<UserRecord>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _userRepository.GetUserByIdAsync(id, cancellationToken);
        return result;
    }
    
    public async Task<ErrorOr<UserRecord>> CreateUserAsync(string name, string email, string password, CancellationToken cancellationToken)
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
            return Domain.Errors.UserErrors.DuplicateEmail;
        }
        
        // Hash the password (BCrypt automatically generates salt)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        // Create the user
        var createResult = await _userRepository.CreateUserAsync(name, email, passwordHash, cancellationToken);
        return createResult;
    }
}

