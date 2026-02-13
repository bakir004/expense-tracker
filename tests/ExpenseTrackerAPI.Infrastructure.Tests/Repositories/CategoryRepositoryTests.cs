using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Categories;
using ExpenseTrackerAPI.Infrastructure.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerAPI.Infrastructure.Tests.Repositories;

[Collection(nameof(DatabaseCollection))]
public class CategoryRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public CategoryRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Category> CreateTestCategoryAsync(string name, string? description = null, string icon = "📁")
    {
        var category = new Category
        {
            Name = name,
            Description = description,
            Icon = icon
        };

        _fixture.DbContext.Categories.Add(category);
        await _fixture.DbContext.SaveChangesAsync();

        return category;
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithCategories_ReturnsCategoriesSortedByName()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        // Create categories in non-alphabetical order
        await CreateTestCategoryAsync("Utilities", "Bills and utilities", "💡");
        await CreateTestCategoryAsync("Food & Dining", "Restaurant and groceries", "🍔");
        await CreateTestCategoryAsync("Transportation", "Travel expenses", "🚗");
        await CreateTestCategoryAsync("Entertainment", "Fun and games", "🎬");

        // Clear the change tracker to ensure we read fresh from database
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(4);

        // Verify sorted by name ascending
        result.Value[0].Name.Should().Be("Entertainment");
        result.Value[1].Name.Should().Be("Food & Dining");
        result.Value[2].Name.Should().Be("Transportation");
        result.Value[3].Name.Should().Be("Utilities");
    }

    [Fact]
    public async Task GetAllAsync_WithCategories_ReturnsAllCategoryProperties()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        await CreateTestCategoryAsync("Salary", "Monthly income", "💰");
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);

        var category = result.Value.First();
        category.Id.Should().BeGreaterThan(0);
        category.Name.Should().Be("Salary");
        category.Description.Should().Be("Monthly income");
        category.Icon.Should().Be("💰");
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleCategories_ReturnsAllCategories()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        // Create multiple categories
        var categoryNames = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
        foreach (var name in categoryNames)
        {
            await CreateTestCategoryAsync(name);
        }

        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(5);

        // All category names should be present
        var returnedNames = result.Value.Select(c => c.Name).ToList();
        returnedNames.Should().Contain(categoryNames);
    }

    [Fact]
    public async Task GetAllAsync_WithNullDescription_ReturnsCategory()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        await CreateTestCategoryAsync("Miscellaneous", null, "📦");
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value.First().Description.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_DoesNotTrackEntities()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        await CreateTestCategoryAsync("Test Category");
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        // Verify that the returned entities are not tracked (AsNoTracking was used)
        var trackedEntities = _fixture.DbContext.ChangeTracker.Entries<Category>().Count();
        trackedEntities.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_MultipleCalls_ReturnsConsistentResults()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new CategoryRepository(_fixture.DbContext);

        await CreateTestCategoryAsync("Category1");
        await CreateTestCategoryAsync("Category2");
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result1 = await sut.GetAllAsync(CancellationToken.None);
        var result2 = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result1.IsError.Should().BeFalse();
        result2.IsError.Should().BeFalse();

        result1.Value.Should().HaveCount(result2.Value.Count);
        result1.Value.Select(c => c.Name).Should().BeEquivalentTo(result2.Value.Select(c => c.Name));
    }
}
