using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.Infrastructure.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExpenseTrackerAPI.WebApi.Tests.Fixtures;

public class ExpenseTrackerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Spins up a REAL Postgres 16 instance in Docker
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("expense_tracker_test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the production DB registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Connect the API to the REAL Postgres container
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Override authentication for tests
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthDefaults.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthDefaults.AuthenticationScheme, _ => { });

            services.AddAuthorization();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Ensure the REAL DB schema is created before tests run
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // Seed users, categories, groups, and transactions
        await DatabaseSeeder.SeedIfEmptyAsync(context);
    }

    public new async Task DisposeAsync() => await _dbContainer.StopAsync();
}
