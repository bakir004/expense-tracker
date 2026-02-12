using ExpenseTrackerAPI.Domain.Errors;
using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Tests.Errors;

public class TransactionErrorsTests
{
    [Fact]
    public void NotFound_HasExpectedTypeAndCode()
    {
        var error = TransactionErrors.NotFound;

        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.Equal("transaction", error.Code);
    }

    [Fact]
    public void InvalidAmount_HasExpectedTypeAndCode()
    {
        var error = TransactionErrors.InvalidAmount;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("amount", error.Code);
    }

    [Fact]
    public void InvalidDate_HasExpectedTypeAndCode()
    {
        var error = TransactionErrors.InvalidDate;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("date", error.Code);
    }

    [Fact]
    public void ExpenseMissingCategory_HasExpectedTypeAndCode()
    {
        var error = TransactionErrors.ExpenseMissingCategory;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("categoryId", error.Code);
    }

    [Fact]
    public void InvalidTransactionType_HasExpectedTypeAndCode()
    {
        var error = TransactionErrors.InvalidTransactionType;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("transactionType", error.Code);
    }

    [Fact]
    public void InvalidSubject_HasExpectedTypeAndCode()
    {
        var error = TransactionErrors.InvalidSubject;

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("subject", error.Code);
    }
}
