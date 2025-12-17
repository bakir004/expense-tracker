using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.ExpenseGroups.Interfaces.Infrastructure;

namespace SampleCkWebApp.Infrastructure.ExpenseGroups;

public static class DependencyInjection
{
    public static IServiceCollection AddExpenseGroupsInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<IExpenseGroupRepository, ExpenseGroupRepository>();
        return services;
    }
}

