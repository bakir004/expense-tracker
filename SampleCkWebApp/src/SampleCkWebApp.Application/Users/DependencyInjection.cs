// ============================================================================
// FILE: DependencyInjection.cs
// ============================================================================
// WHAT: Dependency injection registration extension methods for User application services.
//
// WHY: This file exists in the Application layer to centralize the registration
//      of User-related services. It follows the convention of having a
//      DependencyInjection class per feature/module. This keeps service
//      registration organized and makes it easy to add or remove User
//      functionality. The extension method pattern (AddUsersApplication)
//      makes it discoverable and easy to use in the WebApi layer.
//
// WHAT IT DOES:
//      - Registers IUserService with its implementation (UserService)
//      - Uses TryAddScoped to ensure the service is only registered once
//      - Provides AddUsersApplication() extension method for IServiceCollection
//      - Called from WebApi.DependencyInjection.AddApplication() to wire up
//        all User application services
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Users.Interfaces.Application;

namespace SampleCkWebApp.Application.Users;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.TryAddScoped<IUserService, UserService>();
        return services;
    }
}

