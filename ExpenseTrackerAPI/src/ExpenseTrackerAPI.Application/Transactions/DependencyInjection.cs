using Microsoft.Extensions.DependencyInjection;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;

namespace ExpenseTrackerAPI.Application.Transactions;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionService, TransactionService>();
        return services;
    }
}

