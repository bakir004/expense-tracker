using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Application.Users;
using ExpenseTrackerAPI.Application.Categories;
using ExpenseTrackerAPI.Application.TransactionGroups;
using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Infrastructure.Users;
using ExpenseTrackerAPI.Infrastructure.Categories;
using ExpenseTrackerAPI.Infrastructure.TransactionGroups;
using ExpenseTrackerAPI.Infrastructure.Transactions;
using ExpenseTrackerAPI.Infrastructure.Users.Options;
using ExpenseTrackerAPI.Infrastructure.Shared;

namespace ExpenseTrackerAPI.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddUserOptions(configuration.GetUserOptions());

        return services
            .AddUsersApplication()
            .AddCategoriesApplication()
            .AddTransactionGroupsApplication()
            .AddTransactionServices();
    }
    
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Entity Framework DbContext
        var connectionString = configuration["Database:ConnectionString"];
        services.AddDbContext<ExpenseTrackerDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(30);
                }));

        return services
            .AddUsersInfrastructure(configuration)
            .AddCategoriesInfrastructure()
            .AddTransactionGroupsInfrastructure()
            .AddTransactionInfrastructure();
    }
}