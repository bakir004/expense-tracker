using ErrorOr;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.IntegrationTests;

public class UserRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsersAsync_AfterSeed_ReturnsSeededUsers()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await repo.GetUsersAsync(CancellationToken.None);

        Assert.False(result.IsError);
        var users = result.Value;
        Assert.True(users.Count >= 3);
        Assert.Contains(users, u => u.Email == "john.doe@email.com" && u.Name == "John Doe");
        Assert.Contains(users, u => u.Email == "jane.smith@email.com" && u.Name == "Jane Smith");
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await repo.GetUserByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("john.doe@email.com", result.Value.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExisting_ReturnsNotFound()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await repo.GetUserByIdAsync(99999, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Type == ErrorType.NotFound);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUser()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await repo.GetUserByEmailAsync("jane.smith@email.com", CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("jane.smith@email.com", result.Value.Email);
    }

    [Fact]
    public async Task GetUserBalanceAsync_SeededUser_ReturnsBalance()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await repo.GetUserBalanceAsync(1, CancellationToken.None);

        Assert.False(result.IsError);
        var (initial, cumulative, current) = result.Value;
        Assert.Equal(initial + cumulative, current);
    }

    [Fact]
    public async Task GetUserBalanceAsync_NonExistingUser_ReturnsNotFound()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await repo.GetUserBalanceAsync(99999, CancellationToken.None);

        Assert.True(result.IsError);
    }
}
