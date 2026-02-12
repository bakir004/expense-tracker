using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class TransactionGroupTests
{
    [Fact]
    public void TransactionGroup_CanBeCreated_WithRequiredProperties()
    {
        var now = DateTime.UtcNow;
        var group = new TransactionGroup
        {
            Id = 1,
            Name = "Europe Trip",
            Description = "Summer vacation",
            UserId = 42,
            CreatedAt = now
        };

        Assert.Equal(1, group.Id);
        Assert.Equal("Europe Trip", group.Name);
        Assert.Equal("Summer vacation", group.Description);
        Assert.Equal(42, group.UserId);
        Assert.Equal(now, group.CreatedAt);
    }

    [Fact]
    public void TransactionGroup_DefaultValues_AreSensible()
    {
        var group = new TransactionGroup();

        Assert.Equal(0, group.Id);
        Assert.Equal(string.Empty, group.Name);
        Assert.Null(group.Description);
        Assert.Equal(0, group.UserId);
        Assert.Equal(default(DateTime), group.CreatedAt);
    }
}
