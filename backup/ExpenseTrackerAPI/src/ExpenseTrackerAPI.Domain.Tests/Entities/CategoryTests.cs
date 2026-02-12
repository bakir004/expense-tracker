using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Category_CanBeCreated_WithRequiredProperties()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Food",
            Description = "Groceries and dining",
            Icon = "🍔"
        };

        Assert.Equal(1, category.Id);
        Assert.Equal("Food", category.Name);
        Assert.Equal("Groceries and dining", category.Description);
        Assert.Equal("🍔", category.Icon);
    }

    [Fact]
    public void Category_CanHaveNullOptionalProperties()
    {
        var category = new Category
        {
            Id = 2,
            Name = "Other"
        };

        Assert.Equal(2, category.Id);
        Assert.Equal("Other", category.Name);
        Assert.Null(category.Description);
        Assert.Null(category.Icon);
    }

    [Fact]
    public void Category_DefaultValues_AreSensible()
    {
        var category = new Category();

        Assert.Equal(0, category.Id);
        Assert.Equal(string.Empty, category.Name);
        Assert.Null(category.Description);
        Assert.Null(category.Icon);
    }
}
