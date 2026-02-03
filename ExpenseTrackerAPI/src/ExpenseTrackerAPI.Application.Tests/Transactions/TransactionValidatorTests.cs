using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.Transactions;

public class TransactionValidatorTests
{
    [Fact]
    public void ValidateCreateTransaction_ValidExpense_ShouldReturnSuccess()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date;
        var subject = "Grocery shopping";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateTransaction_ValidIncome_ShouldReturnSuccess()
    {
        // Arrange
        var transactionType = TransactionType.Income;
        var amount = 1000.00m;
        var date = DateTime.UtcNow.Date;
        var subject = "Salary";
        int? categoryId = null;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateTransaction_EmptySubject_ShouldReturnInvalidSubject()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date;
        var subject = "";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidSubject, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_WhitespaceSubject_ShouldReturnInvalidSubject()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date;
        var subject = "   ";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidSubject, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_NullSubject_ShouldReturnInvalidSubject()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date;
        string? subject = null;
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject!, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidSubject, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_ZeroAmount_ShouldReturnInvalidAmount()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 0m;
        var date = DateTime.UtcNow.Date;
        var subject = "Grocery shopping";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidAmount, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_NegativeAmount_ShouldReturnInvalidAmount()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = -10m;
        var date = DateTime.UtcNow.Date;
        var subject = "Grocery shopping";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidAmount, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_DateTooFarInFuture_ShouldReturnInvalidDate()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date.AddDays(2); // More than 1 day in future
        var subject = "Grocery shopping";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidDate, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_DateOneDayInFuture_ShouldReturnSuccess()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date.AddDays(1); // Exactly 1 day in future (allowed)
        var subject = "Grocery shopping";
        var categoryId = 5;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateTransaction_ExpenseWithoutCategory_ShouldReturnExpenseMissingCategory()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 50.00m;
        var date = DateTime.UtcNow.Date;
        var subject = "Grocery shopping";
        int? categoryId = null;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.ExpenseMissingCategory, result.FirstError);
    }

    [Fact]
    public void ValidateCreateTransaction_IncomeWithoutCategory_ShouldReturnSuccess()
    {
        // Arrange
        var transactionType = TransactionType.Income;
        var amount = 1000.00m;
        var date = DateTime.UtcNow.Date;
        var subject = "Salary";
        int? categoryId = null;

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateTransaction_MultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var transactionType = TransactionType.Expense;
        var amount = 0m; // Invalid amount
        var date = DateTime.UtcNow.Date.AddDays(2); // Invalid date
        var subject = ""; // Invalid subject
        int? categoryId = null; // Missing category for expense

        // Act
        var result = TransactionValidator.ValidateCreateTransaction(
            transactionType, amount, date, subject, categoryId);

        // Assert
        Assert.True(result.IsError);
        Assert.True(result.Errors.Count >= 4); // Should have at least 4 errors
    }

    [Fact]
    public void ParseTransactionType_Expense_ShouldReturnExpense()
    {
        // Arrange
        var typeString = "EXPENSE";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Expense, result.Value);
    }

    [Fact]
    public void ParseTransactionType_ExpenseLowerCase_ShouldReturnExpense()
    {
        // Arrange
        var typeString = "expense";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Expense, result.Value);
    }

    [Fact]
    public void ParseTransactionType_ExpenseMixedCase_ShouldReturnExpense()
    {
        // Arrange
        var typeString = "ExPeNsE";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Expense, result.Value);
    }

    [Fact]
    public void ParseTransactionType_Income_ShouldReturnIncome()
    {
        // Arrange
        var typeString = "INCOME";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Income, result.Value);
    }

    [Fact]
    public void ParseTransactionType_IncomeLowerCase_ShouldReturnIncome()
    {
        // Arrange
        var typeString = "income";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Income, result.Value);
    }

    [Fact]
    public void ParseTransactionType_InvalidType_ShouldReturnError()
    {
        // Arrange
        var typeString = "INVALID";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidTransactionType, result.FirstError);
    }

    [Fact]
    public void ParseTransactionType_EmptyString_ShouldReturnError()
    {
        // Arrange
        var typeString = "";

        // Act
        var result = TransactionValidator.ParseTransactionType(typeString);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidTransactionType, result.FirstError);
    }

    // --- BuildQueryOptions ---

    [Fact]
    public void BuildQueryOptions_Null_ShouldReturnDefaultOptions()
    {
        var result = TransactionValidator.BuildQueryOptions(null);

        Assert.False(result.IsError);
        Assert.True(result.Value.SortDescending);
        Assert.Null(result.Value.Subject);
        Assert.Null(result.Value.TransactionType);
        Assert.Null(result.Value.SortBy);
    }

    [Fact]
    public void BuildQueryOptions_EmptyParams_ShouldReturnDefaultOptions()
    {
        var p = new TransactionQueryParameters();

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.True(result.Value.SortDescending);
        Assert.Null(result.Value.Subject);
    }

    [Fact]
    public void BuildQueryOptions_ValidSubject_ShouldSetSubject()
    {
        var p = new TransactionQueryParameters { Subject = "  grocery  " };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.Equal("grocery", result.Value.Subject);
    }

    [Fact]
    public void BuildQueryOptions_ValidTransactionTypeExpense_ShouldSetType()
    {
        var p = new TransactionQueryParameters { TransactionType = "EXPENSE" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Expense, result.Value.TransactionType);
    }

    [Fact]
    public void BuildQueryOptions_ValidTransactionTypeIncome_ShouldSetType()
    {
        var p = new TransactionQueryParameters { TransactionType = "INCOME" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Income, result.Value.TransactionType);
    }

    [Fact]
    public void BuildQueryOptions_InvalidTransactionType_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { TransactionType = "INVALID" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal(TransactionErrors.InvalidTransactionType, result.FirstError);
    }

    [Fact]
    public void BuildQueryOptions_ValidDateFrom_ShouldParseAndSetUtc()
    {
        var p = new TransactionQueryParameters { DateFrom = "01-03-2025" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.DateFromUtc);
        Assert.Equal(2025, result.Value.DateFromUtc!.Value.Year);
        Assert.Equal(3, result.Value.DateFromUtc.Value.Month);
        Assert.Equal(1, result.Value.DateFromUtc.Value.Day);
        Assert.Equal(DateTimeKind.Utc, result.Value.DateFromUtc.Value.Kind);
    }

    [Fact]
    public void BuildQueryOptions_InvalidDateFrom_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { DateFrom = "2025-03-01" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.DateFrom", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_InvalidDateTo_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { DateTo = "invalid" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.DateTo", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_DateFromAfterDateTo_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { DateFrom = "15-06-2025", DateTo = "01-06-2025" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.DateRange", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_ValidPaymentMethods_ShouldSetList()
    {
        var p = new TransactionQueryParameters { PaymentMethods = new[] { 0, 1, 7 } };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.PaymentMethods);
        Assert.Equal(3, result.Value.PaymentMethods!.Count);
        Assert.Equal(PaymentMethod.Cash, result.Value.PaymentMethods[0]);
        Assert.Equal(PaymentMethod.DebitCard, result.Value.PaymentMethods[1]);
        Assert.Equal(PaymentMethod.Other, result.Value.PaymentMethods[2]);
    }

    [Fact]
    public void BuildQueryOptions_InvalidPaymentMethodNegative_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { PaymentMethods = new[] { -1 } };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.PaymentMethods", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_InvalidPaymentMethodOutOfRange_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { PaymentMethods = new[] { 10 } };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.PaymentMethods", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_ValidSortBy_ShouldSetSortBy()
    {
        foreach (var sort in new[] { "subject", "paymentMethod", "category", "amount" })
        {
            var p = new TransactionQueryParameters { SortBy = sort };

            var result = TransactionValidator.BuildQueryOptions(p);

            Assert.False(result.IsError);
            Assert.Equal(sort.ToLowerInvariant(), result.Value.SortBy);
        }
    }

    [Fact]
    public void BuildQueryOptions_InvalidSortBy_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { SortBy = "date" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.SortBy", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_SortDirectionAsc_ShouldSetSortDescendingFalse()
    {
        var p = new TransactionQueryParameters { SortDirection = "asc" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.False(result.Value.SortDescending);
    }

    [Fact]
    public void BuildQueryOptions_SortDirectionDesc_ShouldSetSortDescendingTrue()
    {
        var p = new TransactionQueryParameters { SortDirection = "desc" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.True(result.Value.SortDescending);
    }

    [Fact]
    public void BuildQueryOptions_InvalidSortDirection_ShouldReturnError()
    {
        var p = new TransactionQueryParameters { SortDirection = "forward" };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.True(result.IsError);
        Assert.Equal("TransactionQuery.SortDirection", result.FirstError.Code);
    }

    [Fact]
    public void BuildQueryOptions_ValidCategoryIds_ShouldSetCategoryIds()
    {
        var p = new TransactionQueryParameters { CategoryIds = new[] { 1, 2, 5 } };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.CategoryIds);
        Assert.Equal(3, result.Value.CategoryIds!.Count);
        Assert.Equal(1, result.Value.CategoryIds[0]);
        Assert.Equal(5, result.Value.CategoryIds[2]);
    }

    [Fact]
    public void BuildQueryOptions_AllParamsSet_ShouldMapAll()
    {
        var p = new TransactionQueryParameters
        {
            Subject = "coffee",
            CategoryIds = new[] { 1 },
            PaymentMethods = new[] { 0 },
            TransactionType = "EXPENSE",
            DateFrom = "01-01-2025",
            DateTo = "31-01-2025",
            SortBy = "amount",
            SortDirection = "asc"
        };

        var result = TransactionValidator.BuildQueryOptions(p);

        Assert.False(result.IsError);
        Assert.Equal("coffee", result.Value.Subject);
        Assert.Single(result.Value.CategoryIds!);
        Assert.Single(result.Value.PaymentMethods!);
        Assert.Equal(TransactionType.Expense, result.Value.TransactionType);
        Assert.NotNull(result.Value.DateFromUtc);
        Assert.NotNull(result.Value.DateToUtc);
        Assert.Equal("amount", result.Value.SortBy);
        Assert.False(result.Value.SortDescending);
    }
}

