using ExpenseTrackerAPI.Application.TransactionGroups.Data;
using ExpenseTrackerAPI.Application.TransactionGroups.Mappings;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Tests.TransactionGroups;

public class TransactionGroupMappingsTests
{
    [Fact]
    public void ToResponse_TransactionGroup_ShouldMapAllProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var transactionGroup = new TransactionGroup
        {
            Id = 1,
            Name = "Vacation Trip",
            Description = "Summer vacation to Europe",
            UserId = 10,
            CreatedAt = createdAt
        };

        // Act
        var result = transactionGroup.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transactionGroup.Id, result.Id);
        Assert.Equal(transactionGroup.Name, result.Name);
        Assert.Equal(transactionGroup.Description, result.Description);
        Assert.Equal(transactionGroup.UserId, result.UserId);
        Assert.Equal(transactionGroup.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public void ToResponse_TransactionGroup_ShouldHandleNullDescription()
    {
        // Arrange
        var transactionGroup = new TransactionGroup
        {
            Id = 1,
            Name = "Vacation Trip",
            Description = null,
            UserId = 10,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = transactionGroup.ToResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Description);
    }

    [Fact]
    public void ToResponse_GetTransactionGroupsResult_ShouldMapAllTransactionGroups()
    {
        // Arrange
        var transactionGroups = new List<TransactionGroup>
        {
            new TransactionGroup { Id = 1, Name = "Vacation", Description = "Summer trip", UserId = 10, CreatedAt = DateTime.UtcNow },
            new TransactionGroup { Id = 2, Name = "Renovation", Description = "Home renovation", UserId = 10, CreatedAt = DateTime.UtcNow }
        };
        var result = new GetTransactionGroupsResult { TransactionGroups = transactionGroups };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.TransactionGroups.Count);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(transactionGroups[0].Id, response.TransactionGroups[0].Id);
        Assert.Equal(transactionGroups[0].Name, response.TransactionGroups[0].Name);
        Assert.Equal(transactionGroups[1].Id, response.TransactionGroups[1].Id);
        Assert.Equal(transactionGroups[1].Name, response.TransactionGroups[1].Name);
    }

    [Fact]
    public void ToResponse_GetTransactionGroupsResult_ShouldHandleEmptyList()
    {
        // Arrange
        var result = new GetTransactionGroupsResult { TransactionGroups = new List<TransactionGroup>() };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response.TransactionGroups);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public void ToResponse_GetTransactionGroupsResult_ShouldCalculateTotalCountCorrectly()
    {
        // Arrange
        var transactionGroups = new List<TransactionGroup>
        {
            new TransactionGroup { Id = 1, Name = "Group 1", UserId = 10, CreatedAt = DateTime.UtcNow },
            new TransactionGroup { Id = 2, Name = "Group 2", UserId = 10, CreatedAt = DateTime.UtcNow },
            new TransactionGroup { Id = 3, Name = "Group 3", UserId = 10, CreatedAt = DateTime.UtcNow }
        };
        var result = new GetTransactionGroupsResult { TransactionGroups = transactionGroups };

        // Act
        var response = result.ToResponse();

        // Assert
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(transactionGroups.Count, response.TransactionGroups.Count);
    }
}

