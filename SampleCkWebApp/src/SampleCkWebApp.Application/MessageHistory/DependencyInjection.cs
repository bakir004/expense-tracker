using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.MessageHistory.Interfaces.Application;

namespace SampleCkWebApp.Application.MessageHistory;

public static class DependencyInjection
{
    public static IServiceCollection AddMessageHistoryApplication(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageHistoryService, MessageHistoryService>();
        return services;
    }
}