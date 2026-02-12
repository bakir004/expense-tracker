using ExpenseTrackerAPI.Domain.Errors;
using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Tests.Errors;

public class TransactionGroupErrorsTests
{
    [Fact]
    public void NotFound_HasExpectedTypeAndCode()
    {
        var error = TransactionGroupErrors.NotFound;

        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("transactionGroup", error.Code);
    }

    [Fact]
    public void InvalidName_HasExpectedTypeAndCode()
    {
        var error = TransactionGroupErrors.InvalidName;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("name", error.Code);
    }

    [Fact]
    public void InvalidUserId_HasExpectedTypeAndCode()
    {
        var error = TransactionGroupErrors.InvalidUserId;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("userId", error.Code);
    }

    [Fact]
    public void UserNotFound_HasExpectedTypeAndCode()
    {
        var error = TransactionGroupErrors.UserNotFound;

        Assert.Equal(ErrorType.Failure, error.Type);
        Assert.Equal("userId", error.Code);
    }
}
