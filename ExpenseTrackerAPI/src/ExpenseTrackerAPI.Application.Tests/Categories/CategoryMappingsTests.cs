using ExpenseTrackerAPI.Application.Categories.Data;
using ExpenseTrackerAPI.Application.Categories.Mappings;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Tests.Categories;

public class CategoryMappingsTests
{
    [Fact]
    public void ToResponse_Category_ShouldMapAllProperties()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Description = "Food expenses",
            Icon = "food-icon"
        };

        // Act
        var result = category.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.Id);
        Assert.Equal(category.Name, result.Name);
        Assert.Equal(category.Description, result.Description);
        Assert.Equal(category.Icon, result.Icon);
    }

    [Fact]
    public void ToResponse_Category_ShouldHandleNullDescription()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Description = null,
            Icon = "food-icon"
        };

        // Act
        var result = category.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Description);
    }

    [Fact]
    public void ToResponse_Category_ShouldHandleNullIcon()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Description = "Food expenses",
            Icon = null
        };

        // Act
        var result = category.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Icon);
    }

    [Fact]
    public void ToResponse_GetCategoriesResult_ShouldMapAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Food", Description = "Food expenses", Icon = "🍔" },
            new Category { Id = 2, Name = "Transport", Description = "Transport expenses", Icon = "🚗" }
        };
        var result = new GetCategoriesResult { Categories = categories };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Categories.Count);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(categories[0].Id, response.Categories[0].Id);
        Assert.Equal(categories[0].Name, response.Categories[0].Name);
        Assert.Equal(categories[1].Id, response.Categories[1].Id);
        Assert.Equal(categories[1].Name, response.Categories[1].Name);
    }

    [Fact]
    public void ToResponse_GetCategoriesResult_ShouldHandleEmptyList()
    {
        // Arrange
        var result = new GetCategoriesResult { Categories = new List<Category>() };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Categories);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ToResponse_GetCategoriesResult_ShouldCalculateTotalCountCorrectly()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Food" },
            new Category { Id = 2, Name = "Transport" },
            new Category { Id = 3, Name = "Entertainment" }
        };
        var result = new GetCategoriesResult { Categories = categories };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(categories.Count, response.Categories.Count);
    }
}

