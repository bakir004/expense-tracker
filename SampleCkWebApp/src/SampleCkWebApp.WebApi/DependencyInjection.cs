using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SampleCkWebApp.Application.Users;
using SampleCkWebApp.Application.Categories;
using SampleCkWebApp.Application.TransactionGroups;
using SampleCkWebApp.Application.Transactions;
using SampleCkWebApp.Infrastructure.Users;
using SampleCkWebApp.Infrastructure.Categories;
using SampleCkWebApp.Infrastructure.TransactionGroups;
using SampleCkWebApp.Infrastructure.Transactions;
using SampleCkWebApp.Infrastructure.Users.Options;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.WebApi;

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