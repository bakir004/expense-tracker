using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Application.Transactions.Mappings;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Tests.Transactions;

public class TransactionMappingsTests
{
    [Fact]
    public void ToResponse_Transaction_ShouldMapAllProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-5);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var transaction = new Transaction
        {
            Id = 1,
            UserId = 10,
            TransactionType = TransactionType.Expense,
            Amount = 50.00m,
            SignedAmount = -50.00m,
            CumulativeDelta = -50.00m,
            Date = DateTime.UtcNow.Date,
            Subject = "Grocery shopping",
            Notes = "Weekly groceries",
            PaymentMethod = PaymentMethod.Cash,
            CategoryId = 5,
            TransactionGroupId = 2,
            IncomeSource = null,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var result = transaction.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transaction.Id, result.Id);
        Assert.Equal(transaction.UserId, result.UserId);
        Assert.Equal("EXPENSE", result.TransactionType);
        Assert.Equal(transaction.Amount, result.Amount);
        Assert.Equal(transaction.SignedAmount, result.SignedAmount);
        Assert.Equal(transaction.CumulativeDelta, result.CumulativeDelta);
        Assert.Equal(transaction.Date, result.Date);
        Assert.Equal(transaction.Subject, result.Subject);
        Assert.Equal(transaction.Notes, result.Notes);
        Assert.Equal(transaction.PaymentMethod, result.PaymentMethod);
        Assert.Equal(transaction.CategoryId, result.CategoryId);
        Assert.Equal(transaction.TransactionGroupId, result.TransactionGroupId);
        Assert.Equal(transaction.IncomeSource, result.IncomeSource);
        Assert.Equal(transaction.CreatedAt, result.CreatedAt);
        Assert.Equal(transaction.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ToResponse_Transaction_ShouldMapIncomeType()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = 1,
            UserId = 10,
            TransactionType = TransactionType.Income,
            Amount = 1000.00m,
            SignedAmount = 1000.00m,
            Date = DateTime.UtcNow.Date,
            Subject = "Salary",
            IncomeSource = "ABC Corp"
        };

        // Act
        var result = transaction.ToResponse();

        // Assert
        Assert.Equal("INCOME", result.TransactionType);
        Assert.Equal(transaction.IncomeSource, result.IncomeSource);
    }

    [Fact]
    public void ToResponse_Transaction_ShouldHandleNullOptionalFields()
    {
        // Arrange
        var transaction = new Transaction
        {
            Id = 1,
            UserId = 10,
            TransactionType = TransactionType.Expense,
            Amount = 50.00m,
            SignedAmount = -50.00m,
            Date = DateTime.UtcNow.Date,
            Subject = "Grocery shopping",
            Notes = null,
            PaymentMethod = PaymentMethod.Cash,
            CategoryId = null,
            TransactionGroupId = null,
            IncomeSource = null
        };

        // Act
        var result = transaction.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Notes);
        Assert.Null(result.CategoryId);
        Assert.Null(result.TransactionGroupId);
        Assert.Null(result.IncomeSource);
    }

    [Fact]
    public void ToResponse_GetTransactionsResult_ShouldMapAllTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction { Id = 1, UserId = 10, TransactionType = TransactionType.Expense, Amount = 50m, SignedAmount = -50m, Date = DateTime.UtcNow.Date, Subject = "Food" },
            new Transaction { Id = 2, UserId = 10, TransactionType = TransactionType.Income, Amount = 1000m, SignedAmount = 1000m, Date = DateTime.UtcNow.Date, Subject = "Salary" }
        };
        var result = new GetTransactionsResult
        {
            Transactions = transactions,
            TotalIncome = 1000m,
            TotalExpenses = 50m,
            NetChange = 950m,
            IncomeCount = 1,
            ExpenseCount = 1
        };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Transactions.Count);
        Assert.Equal(2, response.TotalCount);
        Assert.NotNull(response.Summary);
        Assert.Equal(1000m, response.Summary.TotalIncome);
        Assert.Equal(50m, response.Summary.TotalExpenses);
        Assert.Equal(950m, response.Summary.NetChange);
        Assert.Equal(1, response.Summary.IncomeCount);
        Assert.Equal(1, response.Summary.ExpenseCount);
    }

    [Fact]
    public void ToResponse_GetTransactionsResult_ShouldHandleEmptyList()
    {
        // Arrange
        var result = new GetTransactionsResult
        {
            Transactions = new List<Transaction>(),
            TotalIncome = 0m,
            TotalExpenses = 0m,
            NetChange = 0m,
            IncomeCount = 0,
            ExpenseCount = 0
        };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.Transactions);
        Assert.Equal(0, response.TotalCount);
        Assert.NotNull(response.Summary);
        Assert.Equal(0m, response.Summary.TotalIncome);
        Assert.Equal(0m, response.Summary.TotalExpenses);
        Assert.Equal(0m, response.Summary.NetChange);
    }

    [Fact]
    public void ToDatabaseString_Expense_ShouldReturnCorrectString()
    {
        // Arrange
        var type = TransactionType.Expense;

        // Act
        var result = type.ToDatabaseString();

        // Assert
        Assert.Equal("EXPENSE", result);
    }

    [Fact]
    public void ToDatabaseString_Income_ShouldReturnCorrectString()
    {
        // Arrange
        var type = TransactionType.Income;

        // Act
        var result = type.ToDatabaseString();

        // Assert
        Assert.Equal("INCOME", result);
    }

    [Fact]
    public void ToDatabaseString_InvalidValue_ShouldThrowException()
    {
        // Arrange
        var invalidType = (TransactionType)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidType.ToDatabaseString());
    }

    [Fact]
    public void FromDatabaseString_Expense_ShouldReturnCorrectEnum()
    {
        // Arrange
        var value = "EXPENSE";

        // Act
        var result = TransactionMappings.FromDatabaseString(value);

        // Assert
        Assert.Equal(TransactionType.Expense, result);
    }

    [Fact]
    public void FromDatabaseString_ExpenseLowerCase_ShouldReturnCorrectEnum()
    {
        // Arrange
        var value = "expense";

        // Act
        var result = TransactionMappings.FromDatabaseString(value);

        // Assert
        Assert.Equal(TransactionType.Expense, result);
    }

    [Fact]
    public void FromDatabaseString_ExpenseMixedCase_ShouldReturnCorrectEnum()
    {
        // Arrange
        var value = "ExPeNsE";

        // Act
        var result = TransactionMappings.FromDatabaseString(value);

        // Assert
        Assert.Equal(TransactionType.Expense, result);
    }

    [Fact]
    public void FromDatabaseString_Income_ShouldReturnCorrectEnum()
    {
        // Arrange
        var value = "INCOME";

        // Act
        var result = TransactionMappings.FromDatabaseString(value);

        // Assert
        Assert.Equal(TransactionType.Income, result);
    }

    [Fact]
    public void FromDatabaseString_IncomeLowerCase_ShouldReturnCorrectEnum()
    {
        // Arrange
        var value = "income";

        // Act
        var result = TransactionMappings.FromDatabaseString(value);

        // Assert
        Assert.Equal(TransactionType.Income, result);
    }

    [Fact]
    public void FromDatabaseString_InvalidValue_ShouldThrowException()
    {
        // Arrange
        var value = "INVALID";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TransactionMappings.FromDatabaseString(value));
        Assert.Contains("Unknown transaction type", exception.Message);
    }

    [Fact]
    public void FromDatabaseString_NullValue_ShouldThrowException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => TransactionMappings.FromDatabaseString(value!));
    }
}

