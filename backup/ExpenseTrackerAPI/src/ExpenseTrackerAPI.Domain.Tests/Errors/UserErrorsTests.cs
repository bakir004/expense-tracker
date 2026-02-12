using ExpenseTrackerAPI.Domain.Errors;
using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Tests.Errors;

public class UserErrorsTests
{
    [Fact]
    public void NotFound_HasExpectedTypeAndCode()
    {
        var error = UserErrors.NotFound;

        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("user", error.Code);
        Assert.Equal("User not found.", error.Description);
    }

    [Fact]
    public void DuplicateEmail_HasExpectedTypeAndCode()
    {
        var error = UserErrors.DuplicateEmail;

        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal("email", error.Code);
        Assert.Equal("A user with this email already exists.", error.Description);
    }

    [Fact]
    public void InvalidEmail_HasExpectedTypeAndCode()
    {
        var error = UserErrors.InvalidEmail;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("email", error.Code);
    }

    [Fact]
    public void InvalidName_HasExpectedTypeAndCode()
    {
        var error = UserErrors.InvalidName;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("name", error.Code);
    }

    [Fact]
    public void InvalidPassword_HasExpectedTypeAndCode()
    {
        var error = UserErrors.InvalidPassword;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("password", error.Code);
    }
}
