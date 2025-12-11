using ErrorOr;
using SampleCkWebApp.Application.Users.Data;

namespace SampleCkWebApp.Application.Users.Interfaces.Application;

public interface IUserService
{
    Task<ErrorOr<GetUsersResult>> GetUsersAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<UserRecord>> GetUserByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserRecord>> CreateUserAsync(string name, string email, string password, CancellationToken cancellationToken);
}

