// ============================================================================
// FILE: DependencyInjection.cs
// ============================================================================
// WHAT: Dependency injection registration extension methods for UserBalance infrastructure services.
//
// WHY: This file exists in the Infrastructure layer to centralize the registration
//      of UserBalance-related infrastructure components (repository). It follows
//      the same pattern as other features - one DependencyInjection class
//      per feature. This keeps infrastructure setup organized and makes it easy
//      to configure UserBalance persistence. The extension method (AddUserBalancesInfrastructure)
//      is called from WebApi layer to wire up all infrastructure dependencies.
//
// WHAT IT DOES:
//      - Registers IUserBalanceRepository with its PostgreSQL implementation (UserBalanceRepository)
//      - Uses TryAddScoped to ensure services are only registered once
//      - Provides AddUserBalancesInfrastructure() extension method for IServiceCollection
//      - Called from WebApi.DependencyInjection.AddInfrastructure() to set up
//        all UserBalance infrastructure services
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.UserBalances.Interfaces.Infrastructure;

namespace SampleCkWebApp.Infrastructure.UserBalances;

public static class DependencyInjection
{
    public static IServiceCollection AddUserBalancesInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<IUserBalanceRepository, UserBalanceRepository>();
        
        return services;
    }
}

