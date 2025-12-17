using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Expenses.Interfaces.Infrastructure;

namespace SampleCkWebApp.Infrastructure.Expenses;

public static class DependencyInjection
{
    public static IServiceCollection AddExpensesInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<IExpenseRepository, ExpenseRepository>();
        return services;
    }
}

