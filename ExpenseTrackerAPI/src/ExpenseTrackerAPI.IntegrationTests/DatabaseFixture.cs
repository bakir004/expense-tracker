using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ExpenseTrackerAPI.Infrastructure.Shared;
using ExpenseTrackerAPI.Infrastructure.Users;
using ExpenseTrackerAPI.Infrastructure.Categories;
using ExpenseTrackerAPI.Infrastructure.TransactionGroups;
using ExpenseTrackerAPI.Infrastructure.Transactions;
using Testcontainers.PostgreSql;

namespace ExpenseTrackerAPI.IntegrationTests;

/// <summary>
/// Spins up a temporary PostgreSQL container, applies EF Core migrations, and seeds data
/// the same way as local (Migrate + SeedIfEmptyAsync). Provides a scoped service provider
/// for resolving repositories.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("expense_tracker_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private ServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();

        services.AddDbContext<ExpenseTrackerDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.CommandTimeout(30);
            }));

        services.AddUsersInfrastructure(configuration);
        services.AddCategoriesInfrastructure();
        services.AddTransactionGroupsInfrastructure();
        services.AddTransactionInfrastructure();

        _serviceProvider = services.BuildServiceProvider();

        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();
            await context.Database.MigrateAsync();
            await DatabaseSeeder.SeedIfEmptyAsync(context);
        }
    }

    /// <summary>
    /// Create a new scope and return the service provider. Use this to resolve scoped services (repositories).
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    public async Task DisposeAsync() => await _container.DisposeAsync();

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}
