using ExpenseTrackerAPI.Application.Transactions;
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
}

