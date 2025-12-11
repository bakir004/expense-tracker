using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleCkWebApp.Application.MessageHistory;
using SampleCkWebApp.Application.Users;
using SampleCkWebApp.Infrastructure.MessageHistory;
using SampleCkWebApp.Infrastructure.MessageHistory.Options;
using SampleCkWebApp.Infrastructure.Users;
using SampleCkWebApp.Infrastructure.Users.Options;
using SampleCkWebApp.WebApi.Options;

namespace SampleCkWebApp.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddMessageHistoryOptions(configuration.GetMessageHistoryOptions());
        services.TryAddUserOptions(configuration.GetUserOptions());

        return services
            .AddMessageHistoryApplication()
            .AddUsersApplication();
    }
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddMessageHistoryInfrastructure(configuration)
            .AddUsersInfrastructure(configuration);
    }
}