using ExpenseTrackerAPI.Application.Categories;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.Categories;

public class CategoryValidatorTests
{
    [Fact]
    public void ValidateCategoryRequest_ValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Food";
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCategoryRequest_ValidInputWithNullDescription_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Food";
        string? description = null;
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCategoryRequest_ValidInputWithNullIcon_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Food";
        var description = "Food expenses";
        string? icon = null;

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCategoryRequest_EmptyName_ShouldReturnInvalidName()
    {
        // Arrange
        var name = "";
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCategoryRequest_WhitespaceName_ShouldReturnInvalidName()
    {
        // Arrange
        var name = "   ";
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCategoryRequest_NullName_ShouldReturnInvalidName()
    {
        // Arrange
        string? name = null;
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name!, description, icon);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCategoryRequest_NameTooLong_ShouldReturnInvalidName()
    {
        // Arrange
        var name = new string('A', 256); // 256 characters (exceeds 255 limit)
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(CategoryErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCategoryRequest_NameAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var name = new string('A', 255); // Exactly 255 characters
        var description = "Food expenses";
        var icon = "food-icon";

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCategoryRequest_IconTooLong_ShouldReturnValidationError()
    {
        // Arrange
        var name = "Food";
        var description = "Food expenses";
        var icon = new string('A', 101); // 101 characters (exceeds 100 limit)

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Category.Icon", result.FirstError.Code);
        Assert.Contains("100 characters or less", result.FirstError.Description);
    }

    [Fact]
    public void ValidateCategoryRequest_IconAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Food";
        var description = "Food expenses";
        var icon = new string('A', 100); // Exactly 100 characters

        // Act
        var result = CategoryValidator.ValidateCategoryRequest(name, description, icon);

        // Assert
        Assert.False(result.IsError);
    }
}

