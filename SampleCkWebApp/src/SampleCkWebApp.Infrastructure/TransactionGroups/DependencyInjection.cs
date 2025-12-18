using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;

namespace SampleCkWebApp.Infrastructure.TransactionGroups;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionGroupsInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<ITransactionGroupRepository, TransactionGroupRepository>();
        return services;
    }
}

