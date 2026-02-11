using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_CanBeCreated_WithRequiredProperties()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hashed",
            InitialBalance = 100m,
            CreatedAt = now,
            UpdatedAt = now
        };

        Assert.Equal(1, user.Id);
        Assert.Equal("John Doe", user.Name);
        Assert.Equal("john@example.com", user.Email);
        Assert.Equal("hashed", user.PasswordHash);
        Assert.Equal(100m, user.InitialBalance);
        Assert.Equal(now, user.CreatedAt);
        Assert.Equal(now, user.UpdatedAt);
    }

    [Fact]
    public void User_DefaultValues_AreSensible()
    {
        var user = new User();

        Assert.Equal(0, user.Id);
        Assert.Equal(string.Empty, user.Name);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.Equal(0m, user.InitialBalance);
        Assert.Equal(default(DateTime), user.CreatedAt);
        Assert.Equal(default(DateTime), user.UpdatedAt);
    }
}
