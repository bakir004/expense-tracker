// ============================================================================
// FILE: IUserService.cs
// ============================================================================
// WHAT: Interface defining the contract for user application service operations.
//
// WHY: This interface exists in the Application layer to define the public API
//      for user-related business operations. It follows the Interface Segregation
//      Principle by defining only what consumers need. The interface is defined
//      here but implemented in the same layer, allowing for easy testing and
//      potential future implementations (e.g., caching, logging decorators).
//
// WHAT IT DOES:
//      - Defines three core user operations: GetUsersAsync, GetUserByIdAsync,
//        and CreateUserAsync
//      - Uses ErrorOr pattern for functional error handling
//      - Returns domain entities (User) and application DTOs (GetUsersResult)
//      - Acts as the contract that controllers depend on, keeping them
//        decoupled from concrete implementations
// ============================================================================

using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Users.Data;

namespace SampleCkWebApp.Application.Users.Interfaces.Application;

/// <summary>
/// Application service interface for user operations.
/// This service orchestrates domain logic and coordinates with the repository.
/// </summary>
public interface IUserService
{
    Task<ErrorOr<GetUsersResult>> GetUsersAsync(CancellationToken cancellationToken);
    
    Task<ErrorOr<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken);
    
    Task<ErrorOr<User>> CreateUserAsync(string name, string email, string password, CancellationToken cancellationToken);
}

