// ============================================================================
// FILE: DependencyInjection.cs
// ============================================================================
// WHAT: Dependency injection registration extension methods for UserBalance application services.
//
// WHY: This file exists in the Application layer to centralize the registration
//      of UserBalance-related services. It follows the convention of having a
//      DependencyInjection class per feature/module. This keeps service
//      registration organized and makes it easy to add or remove UserBalance
//      functionality. The extension method (AddUserBalancesApplication)
//      makes it discoverable and easy to use in the WebApi layer.
//
// WHAT IT DOES:
//      - Registers IUserBalanceService with its implementation (UserBalanceService)
//      - Uses TryAddScoped to ensure the service is only registered once
//      - Provides AddUserBalancesApplication() extension method for IServiceCollection
//      - Called from WebApi.DependencyInjection.AddApplication() to wire up
//        all UserBalance application services
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.UserBalances.Interfaces.Application;

namespace SampleCkWebApp.Application.UserBalances;

public static class DependencyInjection
{
    public static IServiceCollection AddUserBalancesApplication(this IServiceCollection services)
    {
        services.TryAddScoped<IUserBalanceService, UserBalanceService>();
        return services;
    }
}

