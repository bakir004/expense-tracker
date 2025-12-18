using Microsoft.Extensions.DependencyInjection;
using SampleCkWebApp.Application.Transactions.Interfaces.Infrastructure;

namespace SampleCkWebApp.Infrastructure.Transactions;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        return services;
    }
}

