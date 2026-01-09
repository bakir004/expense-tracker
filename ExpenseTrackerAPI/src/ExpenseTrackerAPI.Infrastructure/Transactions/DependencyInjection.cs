using Microsoft.Extensions.DependencyInjection;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;

namespace ExpenseTrackerAPI.Infrastructure.Transactions;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        return services;
    }
}

