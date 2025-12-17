using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.ExpenseGroups.Interfaces.Application;

namespace SampleCkWebApp.Application.ExpenseGroups;

public static class DependencyInjection
{
    public static IServiceCollection AddExpenseGroupsApplication(this IServiceCollection services)
    {
        services.TryAddScoped<IExpenseGroupService, ExpenseGroupService>();
        return services;
    }
}

