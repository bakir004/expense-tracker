using ErrorOr;
using Moq;
using SampleCkWebApp.Application.TransactionGroups;
using SampleCkWebApp.Application.TransactionGroups.Data;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;

namespace SampleCkWebApp.Application.Tests.TransactionGroups;

public class TransactionGroupServiceTests
{
    private readonly Mock<ITransactionGroupRepository> _mockRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly TransactionGroupService _service;

    public TransactionGroupServiceTests()
    {
        _mockRepository = new Mock<ITransactionGroupRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _service = new TransactionGroupService(_mockRepository.Object, _mockUserRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnTransactionGroups_WhenRepositoryReturnsGroups()
    {
        // Arrange
        var groups = new List<TransactionGroup>
        {
            new TransactionGroup { Id = 1, Name = "Group 1", UserId = 1 },
            new TransactionGroup { Id = 2, Name = "Group 2", UserId = 1 }
        };
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TransactionGroups.Count);
        Assert.Equal(groups, result.Value.TransactionGroups);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnError_WhenRepositoryReturnsError()
    {
        // Arrange
        var error = Error.Failure("database", "Database error");
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(error);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransactionGroup_WhenGroupExists()
    {
        // Arrange
        var group = new TransactionGroup { Id = 1, Name = "Group 1", UserId = 1 };
        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(group.Id, result.Value.Id);
        Assert.Equal(group.Name, result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionGroupErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnTransactionGroups_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var groups = new List<TransactionGroup>
        {
            new TransactionGroup { Id = 1, Name = "Group 1", UserId = userId }
        };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        // Act
        var result = await _service.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(groups, result.Value);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _service.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.NotFound, result.Errors);
        _mockRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTransactionGroup_WhenValidInput()
    {
        // Arrange
        var name = "Group 1";
        var description = "Description";
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var createdGroup = new TransactionGroup
        {
            Id = 1,
            Name = name,
            Description = description,
            UserId = userId
        };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdGroup);

        // Act
        var result = await _service.CreateAsync(name, description, userId, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(description, result.Value.Description);
        Assert.Equal(userId, result.Value.UserId);
        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<TransactionGroup>(g => g.Name == name && g.Description == description && g.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenNameIsEmpty()
    {
        // Arrange
        var name = "";
        var description = "Description";
        var userId = 1;

        // Act
        var result = await _service.CreateAsync(name, description, userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionGroupErrors.InvalidName, result.Errors);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var name = "Group 1";
        var description = "Description";
        var userId = 999;

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _service.CreateAsync(name, description, userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionGroupErrors.UserNotFound, result.Errors);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTransactionGroup_WhenValidInput()
    {
        // Arrange
        var id = 1;
        var name = "Group Updated";
        var description = "Updated description";
        var existingGroup = new TransactionGroup { Id = id, Name = "Group 1", Description = "Old description", UserId = 1 };
        var updatedGroup = new TransactionGroup { Id = id, Name = name, Description = description, UserId = 1 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedGroup);

        // Act
        var result = await _service.UpdateAsync(id, name, description, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(description, result.Value.Description);
        Assert.Equal(existingGroup.UserId, result.Value.UserId); // UserId should be preserved
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        // Arrange
        var id = 999;
        var name = "Group Updated";
        var description = "Updated description";

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _service.UpdateAsync(id, name, description, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionGroupErrors.NotFound, result.Errors);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnDeleted_WhenGroupExists()
    {
        // Arrange
        var id = 1;
        _mockRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Deleted);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _mockRepository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnError_WhenGroupDoesNotExist()
    {
        // Arrange
        var id = 999;
        _mockRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionGroupErrors.NotFound, result.Errors);
    }
}

