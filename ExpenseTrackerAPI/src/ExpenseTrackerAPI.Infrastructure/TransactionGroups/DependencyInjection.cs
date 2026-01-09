using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;

namespace ExpenseTrackerAPI.Infrastructure.TransactionGroups;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionGroupsInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<ITransactionGroupRepository, TransactionGroupRepository>();
        return services;
    }
}

