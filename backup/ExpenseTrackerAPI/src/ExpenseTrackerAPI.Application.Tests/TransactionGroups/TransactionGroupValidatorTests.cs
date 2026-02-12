using ExpenseTrackerAPI.Application.TransactionGroups;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.TransactionGroups;

public class TransactionGroupValidatorTests
{
    [Fact]
    public void ValidateTransactionGroupRequest_ValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Vacation Trip";
        var description = "Summer vacation to Europe";
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_ValidInputWithNullDescription_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Vacation Trip";
        string? description = null;
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_EmptyName_ShouldReturnInvalidName()
    {
        // Arrange
        var name = "";
        var description = "Summer vacation to Europe";
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionGroupErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_WhitespaceName_ShouldReturnInvalidName()
    {
        // Arrange
        var name = "   ";
        var description = "Summer vacation to Europe";
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionGroupErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_NullName_ShouldReturnInvalidName()
    {
        // Arrange
        string? name = null;
        var description = "Summer vacation to Europe";
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name!, description, userId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionGroupErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_NameTooLong_ShouldReturnInvalidName()
    {
        // Arrange
        var name = new string('A', 256); // 256 characters (exceeds 255 limit)
        var description = "Summer vacation to Europe";
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionGroupErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_NameAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var name = new string('A', 255); // Exactly 255 characters
        var description = "Summer vacation to Europe";
        var userId = 10;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_ZeroUserId_ShouldReturnInvalidUserId()
    {
        // Arrange
        var name = "Vacation Trip";
        var description = "Summer vacation to Europe";
        var userId = 0;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionGroupErrors.InvalidUserId, result.FirstError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_NegativeUserId_ShouldReturnInvalidUserId()
    {
        // Arrange
        var name = "Vacation Trip";
        var description = "Summer vacation to Europe";
        var userId = -1;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TransactionGroupErrors.InvalidUserId, result.FirstError);
    }

    [Fact]
    public void ValidateTransactionGroupRequest_PositiveUserId_ShouldReturnSuccess()
    {
        // Arrange
        var name = "Vacation Trip";
        var description = "Summer vacation to Europe";
        var userId = 1;

        // Act
        var result = TransactionGroupValidator.ValidateTransactionGroupRequest(name, description, userId);

        // Assert
        Assert.False(result.IsError);
    }
}

