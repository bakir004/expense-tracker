using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    public void Transaction_CanBeCreated_WithRequiredProperties()
    {
        var now = DateTime.UtcNow;
        var date = DateTime.UtcNow.Date;
        var transaction = new Transaction
        {
            Id = 1,
            UserId = 10,
            TransactionType = TransactionType.Expense,
            Amount = 50m,
            SignedAmount = -50m,
            CumulativeDelta = 100m,
            Date = date,
            Subject = "Coffee",
            PaymentMethod = PaymentMethod.Cash,
            CategoryId = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        Assert.Equal(1, transaction.Id);
        Assert.Equal(10, transaction.UserId);
        Assert.Equal(TransactionType.Expense, transaction.TransactionType);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal(-50m, transaction.SignedAmount);
        Assert.Equal(100m, transaction.CumulativeDelta);
        Assert.Equal(date, transaction.Date);
        Assert.Equal("Coffee", transaction.Subject);
        Assert.Equal(PaymentMethod.Cash, transaction.PaymentMethod);
        Assert.Equal(1, transaction.CategoryId);
        Assert.Null(transaction.Notes);
        Assert.Null(transaction.TransactionGroupId);
        Assert.Null(transaction.IncomeSource);
        Assert.Null(transaction.BalanceAfter);
    }

    [Fact]
    public void Transaction_DefaultValues_AreSensible()
    {
        var transaction = new Transaction();

        Assert.Equal(0, transaction.Id);
        Assert.Equal(0, transaction.UserId);
        Assert.Equal(0m, transaction.Amount);
        Assert.Equal(0m, transaction.SignedAmount);
        Assert.Equal(0m, transaction.CumulativeDelta);
        Assert.Equal(string.Empty, transaction.Subject);
        Assert.Null(transaction.CategoryId);
        Assert.Null(transaction.TransactionGroupId);
        Assert.Null(transaction.IncomeSource);
        Assert.Null(transaction.BalanceAfter);
    }
}
