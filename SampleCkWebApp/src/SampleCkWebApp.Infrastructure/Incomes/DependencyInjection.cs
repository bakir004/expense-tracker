using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Incomes.Interfaces.Infrastructure;

namespace SampleCkWebApp.Infrastructure.Incomes;

public static class DependencyInjection
{
    public static IServiceCollection AddIncomesInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<IIncomeRepository, IncomeRepository>();
        return services;
    }
}

