using Microsoft.Extensions.DependencyInjection;
using SampleCkWebApp.Application.Transactions.Interfaces.Application;

namespace SampleCkWebApp.Application.Transactions;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionServices(this IServiceCollection services)
    {
        services.AddScoped<ITransactionService, TransactionService>();
        return services;
    }
}

