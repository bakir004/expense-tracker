using ExpenseTrackerAPI.Domain.Errors;
using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Tests.Errors;

public class CategoryErrorsTests
{
    [Fact]
    public void NotFound_HasExpectedTypeAndCode()
    {
        var error = CategoryErrors.NotFound;

        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("category", error.Code);
        Assert.Equal("Category not found.", error.Description);
    }

    [Fact]
    public void DuplicateName_HasExpectedTypeAndCode()
    {
        var error = CategoryErrors.DuplicateName;

        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal("name", error.Code);
    }

    [Fact]
    public void InvalidName_HasExpectedTypeAndCode()
    {
        var error = CategoryErrors.InvalidName;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("name", error.Code);
    }
}
