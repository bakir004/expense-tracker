using ErrorOr;
using ExpenseTrackerAPI.Application.Categories;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ExpenseTrackerAPI.Application.Tests.Categories;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _categoryService = new CategoryService(_mockCategoryRepository.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCategoryRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CategoryService(null!);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("categoryRepository");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsCategories_ShouldReturnCategories()
    {
        // Arrange
        var expectedCategories = new List<Category>
        {
            new Category { Id = 1, Name = "Food & Dining", Description = "Restaurant and groceries", Icon = "🍔" },
            new Category { Id = 2, Name = "Transportation", Description = "Travel expenses", Icon = "🚗" },
            new Category { Id = 3, Name = "Entertainment", Description = "Fun and games", Icon = "🎬" }
        };

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCategories);

        // Act
        var result = await _categoryService.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeEquivalentTo(expectedCategories);

        _mockCategoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyCategories = new List<Category>();

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCategories);

        // Act
        var result = await _categoryService.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();

        _mockCategoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsError_ShouldReturnError()
    {
        // Arrange
        var expectedError = Error.Failure("Database.Error", "Failed to retrieve categories");

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        // Act
        var result = await _categoryService.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expectedError);

        _mockCategoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(token))
            .ReturnsAsync(new List<Category>());

        // Act
        await _categoryService.GetAllAsync(token);

        // Assert
        _mockCategoryRepository.Verify(r => r.GetAllAsync(token), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCancelled_ShouldPropagateCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _categoryService.GetAllAsync(cts.Token));
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsSingleCategory_ShouldReturnSingleCategory()
    {
        // Arrange
        var singleCategory = new List<Category>
        {
            new Category { Id = 1, Name = "Salary", Description = "Monthly income", Icon = "💰" }
        };

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(singleCategory);

        // Act
        var result = await _categoryService.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("Salary");
        result.Value.First().Description.Should().Be("Monthly income");
        result.Value.First().Icon.Should().Be("💰");
    }

    [Fact]
    public async Task GetAllAsync_WhenCategoryHasNullDescription_ShouldReturnCategoryWithNullDescription()
    {
        // Arrange
        var categoriesWithNullDescription = new List<Category>
        {
            new Category { Id = 1, Name = "Miscellaneous", Description = null, Icon = "📦" }
        };

        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoriesWithNullDescription);

        // Act
        var result = await _categoryService.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value.First().Description.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_MultipleCalls_ShouldCallRepositoryEachTime()
    {
        // Arrange
        _mockCategoryRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        // Act
        await _categoryService.GetAllAsync(CancellationToken.None);
        await _categoryService.GetAllAsync(CancellationToken.None);
        await _categoryService.GetAllAsync(CancellationToken.None);

        // Assert
        _mockCategoryRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    #endregion
}
