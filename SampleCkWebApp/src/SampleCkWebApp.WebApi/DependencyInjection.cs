using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SampleCkWebApp.Application.Users;
using SampleCkWebApp.Application.UserBalances;
using SampleCkWebApp.Application.Categories;
using SampleCkWebApp.Application.ExpenseGroups;
using SampleCkWebApp.Application.Expenses;
using SampleCkWebApp.Application.Incomes;
using SampleCkWebApp.Infrastructure.Users;
using SampleCkWebApp.Infrastructure.UserBalances;
using SampleCkWebApp.Infrastructure.Categories;
using SampleCkWebApp.Infrastructure.ExpenseGroups;
using SampleCkWebApp.Infrastructure.Expenses;
using SampleCkWebApp.Infrastructure.Incomes;
using SampleCkWebApp.Infrastructure.Users.Options;
using SampleCkWebApp.WebApi.Options;

namespace SampleCkWebApp.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddUserOptions(configuration.GetUserOptions());

        return services
            .AddUsersApplication()
            .AddUserBalancesApplication()
            .AddCategoriesApplication()
            .AddExpenseGroupsApplication()
            .AddExpensesApplication()
            .AddIncomesApplication();
    }
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddUsersInfrastructure(configuration)
            .AddUserBalancesInfrastructure()
            .AddCategoriesInfrastructure()
            .AddExpenseGroupsInfrastructure()
            .AddExpensesInfrastructure()
            .AddIncomesInfrastructure();
    }
}