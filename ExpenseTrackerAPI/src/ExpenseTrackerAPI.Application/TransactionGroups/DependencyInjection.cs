using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;

namespace ExpenseTrackerAPI.Application.TransactionGroups;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionGroupsApplication(this IServiceCollection services)
    {
        services.TryAddScoped<ITransactionGroupService, TransactionGroupService>();
        return services;
    }
}

