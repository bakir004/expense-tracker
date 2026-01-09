using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ExpenseTrackerAPI.Application.Users;
using ExpenseTrackerAPI.Application.Categories;
using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Application.TransactionGroups;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Application;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;
using ExpenseTrackerAPI.WebApi;

namespace ExpenseTrackerAPI.Application.Tests.DependencyInjection;

public class DependencyInjectionTests
{
    [Fact]
    public void AddUsersApplication_ShouldRegisterIUserService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUsersApplication();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(UserService), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddUsersApplication_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUsersApplication();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserService));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddUsersApplication_ShouldNotRegisterMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUsersApplication();
        services.AddUsersApplication(); // Call twice

        // Assert
        var descriptors = services.Where(s => s.ServiceType == typeof(IUserService)).ToList();
        Assert.Single(descriptors); // Should only be registered once
    }

    [Fact]
    public void AddCategoriesApplication_ShouldRegisterICategoryService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCategoriesApplication();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICategoryService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(CategoryService), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddCategoriesApplication_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCategoriesApplication();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICategoryService));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddTransactionServices_ShouldRegisterITransactionService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionServices();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TransactionService), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddTransactionServices_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionServices();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionService));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddTransactionGroupsApplication_ShouldRegisterITransactionGroupService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionGroupsApplication();

        // Assert - Verify service is registered
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionGroupService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TransactionGroupService), descriptor!.ImplementationType);
    }

    [Fact]
    public void AddTransactionGroupsApplication_ShouldRegisterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTransactionGroupsApplication();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionGroupService));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public void AddApplication_ShouldRegisterAllApplicationServices()
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
        ExpenseTrackerAPI.WebApi.DependencyInjection.AddApplication(services, configuration);

        // Assert - Verify all services are registered
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(IUserService)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(ICategoryService)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionService)));
        Assert.NotNull(services.FirstOrDefault(s => s.ServiceType == typeof(ITransactionGroupService)));
    }

    [Fact]
    public void AddApplication_ShouldReturnSameServiceCollection()
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
        var result = ExpenseTrackerAPI.WebApi.DependencyInjection.AddApplication(services, configuration);

        // Assert
        Assert.Same(services, result);
    }
}

