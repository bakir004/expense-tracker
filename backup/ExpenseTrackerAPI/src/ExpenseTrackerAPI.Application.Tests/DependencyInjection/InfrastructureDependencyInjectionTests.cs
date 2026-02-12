using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Infrastructure.Users;
using ExpenseTrackerAPI.Infrastructure.Categories;
using ExpenseTrackerAPI.Infrastructure.Transactions;
using ExpenseTrackerAPI.Infrastructure.TransactionGroups;
using ExpenseTrackerAPI.Infrastructure.Shared;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Infrastructure.Users.Options;

namespace ExpenseTrackerAPI.Application.Tests.DependencyInjection;

public class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddUsersInfrastructure_ShouldRegisterIUserRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test" }
            })
            .Build();

        // Act
        services.AddUsersInfrastructure(configuration);

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(UserRepository), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddUsersInfrastructure_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test" }
            })
            .Build();

        // Act
        services.AddUsersInfrastructure(configuration);

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddUsersInfrastructure_ShouldRegisterUserOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test" }
            })
            .Build();

        // Act
        services.AddUsersInfrastructure(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var userOptions = serviceProvider.GetService<UserOptions>();
        
        Assert.NotNull(userOptions);
    }

    [Fact]
    public void AddCategoriesInfrastructure_ShouldRegisterICategoryRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCategoriesInfrastructure();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICategoryRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(CategoryRepository), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddCategoriesInfrastructure_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCategoriesInfrastructure();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICategoryRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddTransactionInfrastructure_ShouldRegisterITransactionRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionInfrastructure();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TransactionRepository), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddTransactionInfrastructure_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionInfrastructure();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddTransactionGroupsInfrastructure_ShouldRegisterITransactionGroupRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionGroupsInfrastructure();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionGroupRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TransactionGroupRepository), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddTransactionGroupsInfrastructure_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionGroupsInfrastructure();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionGroupRepository));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test" }
            })
            .Build();

        // Act
        ExpenseTrackerAPI.WebApi.DependencyInjection.AddInfrastructure(services, configuration);

        // Assert - Verify DbContext is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ExpenseTrackerDbContext));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterAllRepositories()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test" }
            })
            .Build();

        // Act
        ExpenseTrackerAPI.WebApi.DependencyInjection.AddInfrastructure(services, configuration);

        // Assert - Verify all repositories are registered
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(IUserRepository)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(ICategoryRepository)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionRepository)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionGroupRepository)));
    }

    [Fact]
    public void AddInfrastructure_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test" }
            })
            .Build();

        // Act
        var result = ExpenseTrackerAPI.WebApi.DependencyInjection.AddInfrastructure(services, configuration);

        // Assert
        Assert.Same(services, result);
    }
}

