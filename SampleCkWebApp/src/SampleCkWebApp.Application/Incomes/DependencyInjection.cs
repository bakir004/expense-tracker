using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Incomes.Interfaces.Application;

namespace SampleCkWebApp.Application.Incomes;

public static class DependencyInjection
{
    public static IServiceCollection AddIncomesApplication(this IServiceCollection services)
    {
        services.TryAddScoped<IIncomeService, IncomeService>();
        return services;
    }
}

