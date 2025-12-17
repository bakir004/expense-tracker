using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Expenses.Interfaces.Application;

namespace SampleCkWebApp.Application.Expenses;

public static class DependencyInjection
{
    public static IServiceCollection AddExpensesApplication(this IServiceCollection services)
    {
        services.TryAddScoped<IExpenseService, ExpenseService>();
        return services;
    }
}

