using ErrorOr;
using SampleCkWebApp.Application.Users.Data;

namespace SampleCkWebApp.Application.Users.Interfaces.Infrastructure;

public interface IUserRepository
{
    Task<ErrorOr<List<UserRecord>>> GetUsersAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<UserRecord>> GetUserByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserRecord>> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserRecord>> CreateUserAsync(string name, string email, string passwordHash, CancellationToken cancellationToken);
}

