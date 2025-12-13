// ============================================================================
// FILE: DependencyInjection.cs
// ============================================================================
// WHAT: Dependency injection registration extension methods for User infrastructure services.
//
// WHY: This file exists in the Infrastructure layer to centralize the registration
//      of User-related infrastructure components (repository, options). It follows
//      the same pattern as Application layer DI - one DependencyInjection class
//      per feature. This keeps infrastructure setup organized and makes it easy
//      to configure User persistence. The extension method (AddUsersInfrastructure)
//      is called from WebApi layer to wire up all infrastructure dependencies.
//
// WHAT IT DOES:
//      - Registers UserOptions from configuration (database connection string)
//      - Registers IUserRepository with its PostgreSQL implementation (UserRepository)
//      - Uses TryAddScoped to ensure services are only registered once
//      - Provides AddUsersInfrastructure() extension method for IServiceCollection
//      - Called from WebApi.DependencyInjection.AddInfrastructure() to set up
//        all User infrastructure services
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.Users;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddUserOptions(configuration.GetUserOptions());
        
        services.TryAddScoped<IUserRepository, UserRepository>();
        
        return services;
    }
}

