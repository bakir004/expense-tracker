using ErrorOr;
using Moq;
using ExpenseTrackerAPI.Application.Categories;
using ExpenseTrackerAPI.Application.Categories.Data;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.Categories;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _mockRepository;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _mockRepository = new Mock<ICategoryRepository>();
        _service = new CategoryService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCategories_WhenRepositoryReturnsCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Food", Description = "Food expenses" },
            new Category { Id = 2, Name = "Transport", Description = "Transport expenses" }
        };
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Categories.Count);
        Assert.Equal(categories, result.Value.Categories);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnError_WhenRepositoryReturnsError()
    {
        // Arrange
        var error = Error.Failure("database", "Database error");
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(error);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Food", Description = "Food expenses" };
        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(category.Id, result.Value.Id);
        Assert.Equal(category.Name, result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CategoryErrors.NotFound);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCategory_WhenValidInput()
    {
        // Arrange
        var name = "Food";
        var description = "Food expenses";
h        var icon = "food-icon";
        var createdCategory = new Category
        {
            Id = 1,
            Name = name,
            Description = description,
            Icon = icon
        };

        _mockRepository
            .Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("category", "Category not found"));

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _service.CreateAsync(name, description, icon, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(description, result.Value.Description);
        Assert.Equal(icon, result.Value.Icon);
        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<Category>(c => c.Name == name && c.Description == description && c.Icon == icon),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenNameIsEmpty()
    {
        // Arrange
        var name = "";
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = await _service.CreateAsync(name, description, icon, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.InvalidName, result.Errors);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDuplicateNameError_WhenNameAlreadyExists()
    {
        // Arrange
        var name = "Food";
        var description = "Food expenses";
        var icon = "food-icon";
        var existingCategory = new Category { Id = 1, Name = name };

        _mockRepository
            .Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _service.CreateAsync(name, description, icon, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.DuplicateName, result.Errors);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnCategory_WhenValidInput()
    {
        // Arrange
        var id = 1;
        var name = "Food Updated";
        var description = "Updated food expenses";
        var icon = "🍕";
        var existingCategory = new Category { Id = id, Name = "Food", Description = "Food expenses" };
        var updatedCategory = new Category { Id = id, Name = name, Description = description, Icon = icon };

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _mockRepository
            .Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("category", "Category not found"));

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);

        // Act
        var result = await _service.UpdateAsync(id, name, description, icon, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(description, result.Value.Description);
        Assert.Equal(icon, result.Value.Icon);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var id = 999;
        var name = "Food";
        var description = "Food expenses";
        var icon = "food-icon";

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CategoryErrors.NotFound);

        // Act
        var result = await _service.UpdateAsync(id, name, description, icon, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.NotFound, result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotCheckDuplicate_WhenNameUnchanged()
    {
        // Arrange
        var id = 1;
        var name = "Food";
        var description = "Updated description";
        var icon = "food-icon";
        var existingCategory = new Category { Id = id, Name = name, Description = "Old description" };
        var updatedCategory = new Category { Id = id, Name = name, Description = description, Icon = icon };

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCategory);

        // Act
        var result = await _service.UpdateAsync(id, name, description, icon, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _mockRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnDuplicateNameError_WhenNewNameAlreadyExists()
    {
        // Arrange
        var id = 1;
        var name = "Food Updated";
        var description = "Food expenses";
        var icon = "food-icon";
        var existingCategory = new Category { Id = id, Name = "Food", Description = "Food expenses" };
        var duplicateCategory = new Category { Id = 2, Name = name };

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _mockRepository
            .Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateCategory);

        // Act
        var result = await _service.UpdateAsync(id, name, description, icon, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.DuplicateName, result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnDeleted_WhenCategoryExists()
    {
        // Arrange
        var id = 1;
        _mockRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Deleted);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _mockRepository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnError_WhenCategoryDoesNotExist()
    {
        // Arrange
        var id = 999;
        _mockRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CategoryErrors.NotFound);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.NotFound, result.Errors);
    }
}

