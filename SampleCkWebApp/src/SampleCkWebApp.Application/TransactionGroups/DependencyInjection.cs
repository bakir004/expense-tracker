using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Application;

namespace SampleCkWebApp.Application.TransactionGroups;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionGroupsApplication(this IServiceCollection services)
    {
        services.TryAddScoped<ITransactionGroupService, TransactionGroupService>();
        return services;
    }
}

