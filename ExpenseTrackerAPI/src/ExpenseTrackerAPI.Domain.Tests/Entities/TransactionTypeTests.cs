using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class TransactionTypeTests
{
    [Fact]
    public void TransactionType_HasExpectedValues()
    {
        Assert.Equal(0, (int)TransactionType.Expense);
        Assert.Equal(1, (int)TransactionType.Income);
    }

    [Theory]
    [InlineData(TransactionType.Expense)]
    [InlineData(TransactionType.Income)]
    public void TransactionType_AllValues_CanBeUsed(TransactionType type)
    {
        var _ = type;
        Assert.True(Enum.IsDefined(typeof(TransactionType), type));
    }
}
