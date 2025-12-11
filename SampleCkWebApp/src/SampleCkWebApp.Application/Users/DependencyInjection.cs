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

