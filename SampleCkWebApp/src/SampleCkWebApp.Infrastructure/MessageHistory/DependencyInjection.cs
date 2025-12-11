using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.MessageHistory.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.MessageHistory.Options;

namespace SampleCkWebApp.Infrastructure.MessageHistory;

public static class DependencyInjection
{
    public static IServiceCollection AddMessageHistoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddMessageHistoryOptions(configuration.GetMessageHistoryOptions());
        
        services.TryAddSingleton<IMessageHistoryRepository, MessageHistoryRepository>();
        
        return services;
    }
}