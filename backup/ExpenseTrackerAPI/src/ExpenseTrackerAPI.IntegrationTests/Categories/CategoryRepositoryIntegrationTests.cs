using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.IntegrationTests;

public class CategoryRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public CategoryRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_AfterSeed_ReturnsSeededCategories()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var result = await repo.GetAllAsync(CancellationToken.None);

        Assert.False(result.IsError);
        var categories = result.Value;
        Assert.True(categories.Count >= 12);
        Assert.Contains(categories, c => c.Name == "Food & Dining");
        Assert.Contains(categories, c => c.Name == "Salary");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCategory_ReturnsCategory()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var result = await repo.GetByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("Food & Dining", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNotFound()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var result = await repo.GetByIdAsync(99999, CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsCategory()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var result = await repo.GetByNameAsync("Transportation", CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("Transportation", result.Value.Name);
    }
}
