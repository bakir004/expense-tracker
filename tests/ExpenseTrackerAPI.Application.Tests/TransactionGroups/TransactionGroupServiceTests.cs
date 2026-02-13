using ErrorOr;
using ExpenseTrackerAPI.Application.TransactionGroups;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using FluentAssertions;
using Moq;
using Xunit;

namespace ExpenseTrackerAPI.Application.Tests.TransactionGroups;

public class TransactionGroupServiceTests
{
    private readonly Mock<ITransactionGroupRepository> _mockTransactionGroupRepository;
    private readonly TransactionGroupService _transactionGroupService;

    public TransactionGroupServiceTests()
    {
        _mockTransactionGroupRepository = new Mock<ITransactionGroupRepository>();
        _transactionGroupService = new TransactionGroupService(_mockTransactionGroupRepository.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullTransactionGroupRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new TransactionGroupService(null!);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("transactionGroupRepository");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingGroupOwnedByUser_ShouldReturnGroup()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;

        var expectedGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Vacation Trip",
            Description = "Summer vacation expenses",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _transactionGroupService.GetByIdAsync(groupId, userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(groupId);
        result.Value.Name.Should().Be("Vacation Trip");
        result.Value.UserId.Should().Be(userId);

        _mockTransactionGroupRepository.Verify(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingGroupNotOwnedByUser_ShouldReturnNotFound()
    {
        // Arrange
        const int groupId = 1;
        const int ownerUserId = 10;
        const int requestingUserId = 20; // Different user

        var group = new TransactionGroup
        {
            Id = groupId,
            Name = "Other User's Group",
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act
        var result = await _transactionGroupService.GetByIdAsync(groupId, requestingUserId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentGroup_ShouldReturnNotFound()
    {
        // Arrange
        const int groupId = 99999;
        const int userId = 10;

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _transactionGroupService.GetByIdAsync(groupId, userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        const int groupId = 1;
        const int userId = 10;

        var group = new TransactionGroup
        {
            Id = groupId,
            Name = "Test",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, token))
            .ReturnsAsync(group);

        // Act
        await _transactionGroupService.GetByIdAsync(groupId, userId, token);

        // Assert
        _mockTransactionGroupRepository.Verify(r => r.GetByIdAsync(groupId, token), Times.Once);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_UserWithGroups_ShouldReturnGroups()
    {
        // Arrange
        const int userId = 10;

        var expectedGroups = new List<TransactionGroup>
        {
            new TransactionGroup { Id = 1, Name = "Group A", UserId = userId, CreatedAt = DateTime.UtcNow },
            new TransactionGroup { Id = 2, Name = "Group B", UserId = userId, CreatedAt = DateTime.UtcNow },
            new TransactionGroup { Id = 3, Name = "Group C", UserId = userId, CreatedAt = DateTime.UtcNow }
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroups);

        // Act
        var result = await _transactionGroupService.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeEquivalentTo(expectedGroups);

        _mockTransactionGroupRepository.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_UserWithNoGroups_ShouldReturnEmptyList()
    {
        // Arrange
        const int userId = 10;

        _mockTransactionGroupRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TransactionGroup>());

        // Act
        var result = await _transactionGroupService.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenRepositoryReturnsError_ShouldReturnError()
    {
        // Arrange
        const int userId = 10;
        var expectedError = Error.Failure("Database.Error", "Failed to retrieve transaction groups");

        _mockTransactionGroupRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        // Act
        var result = await _transactionGroupService.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expectedError);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidInput_ShouldCreateAndReturnGroup()
    {
        // Arrange
        const int userId = 10;
        const string name = "New Project";
        const string description = "Project description";

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) =>
            {
                g.Id = 1; // Simulate ID assignment
                return g;
            });

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, name, description, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(name);
        result.Value.Description.Should().Be(description);
        result.Value.UserId.Should().Be(userId);

        _mockTransactionGroupRepository.Verify(
            r => r.CreateAsync(It.Is<TransactionGroup>(g =>
                g.Name == name &&
                g.Description == description &&
                g.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_ShouldCreateGroupWithNullDescription()
    {
        // Arrange
        const int userId = 10;
        const string name = "No Description Group";

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) =>
            {
                g.Id = 1;
                return g;
            });

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, name, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithWhitespaceDescription_ShouldTrimToNull()
    {
        // Arrange
        const int userId = 10;
        const string name = "Group Name";
        const string whitespaceDescription = "   ";

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) =>
            {
                g.Id = 1;
                return g;
            });

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, name, whitespaceDescription, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_WithEmptyOrWhitespaceName_ShouldReturnInvalidNameError(string? invalidName)
    {
        // Arrange
        const int userId = 10;

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, invalidName!, "Description", CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Name");

        _mockTransactionGroupRepository.Verify(
            r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNameTooLong_ShouldReturnInvalidNameError()
    {
        // Arrange
        const int userId = 10;
        var tooLongName = new string('A', 256); // 256 characters, limit is 255

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, tooLongName, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Name");

        _mockTransactionGroupRepository.Verify(
            r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNameAtMaxLength_ShouldSucceed()
    {
        // Arrange
        const int userId = 10;
        var maxLengthName = new string('A', 255); // Exactly 255 characters

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) =>
            {
                g.Id = 1;
                return g;
            });

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, maxLengthName, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(maxLengthName);
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimName()
    {
        // Arrange
        const int userId = 10;
        const string nameWithWhitespace = "  Trimmed Name  ";

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) =>
            {
                g.Id = 1;
                return g;
            });

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, nameWithWhitespace, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Trimmed Name");
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimDescription()
    {
        // Arrange
        const int userId = 10;
        const string name = "Test Group";
        const string descriptionWithWhitespace = "  Trimmed Description  ";

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) =>
            {
                g.Id = 1;
                return g;
            });

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, name, descriptionWithWhitespace, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().Be("Trimmed Description");
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryReturnsError_ShouldReturnError()
    {
        // Arrange
        const int userId = 10;
        var expectedError = Error.Failure("Database.Error", "Failed to create transaction group");

        _mockTransactionGroupRepository
            .Setup(r => r.CreateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        // Act
        var result = await _transactionGroupService.CreateAsync(userId, "Valid Name", null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expectedError);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidInputOwnedByUser_ShouldUpdateAndReturnGroup()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;
        const string newName = "Updated Name";
        const string newDescription = "Updated Description";

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Original Name",
            Description = "Original Description",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        _mockTransactionGroupRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) => g);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, userId, newName, newDescription, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(newName);
        result.Value.Description.Should().Be(newDescription);

        _mockTransactionGroupRepository.Verify(
            r => r.UpdateAsync(It.Is<TransactionGroup>(g =>
                g.Id == groupId &&
                g.Name == newName &&
                g.Description == newDescription),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_GroupNotOwnedByUser_ShouldReturnNotFound()
    {
        // Arrange
        const int groupId = 1;
        const int ownerUserId = 10;
        const int requestingUserId = 20;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Other User's Group",
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, requestingUserId, "New Name", null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);

        _mockTransactionGroupRepository.Verify(
            r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentGroup_ShouldReturnNotFound()
    {
        // Arrange
        const int groupId = 99999;
        const int userId = 10;

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, userId, "New Name", null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);

        _mockTransactionGroupRepository.Verify(
            r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdateAsync_WithEmptyOrWhitespaceName_ShouldReturnInvalidNameError(string? invalidName)
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Original",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, userId, invalidName!, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("Name");

        _mockTransactionGroupRepository.Verify(
            r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNameTooLong_ShouldReturnInvalidNameError()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;
        var tooLongName = new string('A', 256);

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Original",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, userId, tooLongName, null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);

        _mockTransactionGroupRepository.Verify(
            r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CanSetDescriptionToNull()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Original",
            Description = "Has Description",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        _mockTransactionGroupRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) => g);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, userId, "New Name", null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldTrimNameAndDescription()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Original",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        _mockTransactionGroupRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TransactionGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroup g, CancellationToken _) => g);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, userId, "  Trimmed Name  ", "  Trimmed Description  ", CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Trimmed Name");
        result.Value.Description.Should().Be("Trimmed Description");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidGroupOwnedByUser_ShouldDeleteAndReturnDeleted()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "To Be Deleted",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        _mockTransactionGroupRepository
            .Setup(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Deleted);

        // Act
        var result = await _transactionGroupService.DeleteAsync(groupId, userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        _mockTransactionGroupRepository.Verify(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GroupNotOwnedByUser_ShouldReturnNotFound()
    {
        // Arrange
        const int groupId = 1;
        const int ownerUserId = 10;
        const int requestingUserId = 20;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Other User's Group",
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.DeleteAsync(groupId, requestingUserId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);

        _mockTransactionGroupRepository.Verify(
            r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentGroup_ShouldReturnNotFound()
    {
        // Arrange
        const int groupId = 99999;
        const int userId = 10;

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _transactionGroupService.DeleteAsync(groupId, userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);

        _mockTransactionGroupRepository.Verify(
            r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryDeleteFails_ShouldReturnError()
    {
        // Arrange
        const int groupId = 1;
        const int userId = 10;
        var expectedError = Error.Failure("Database.Error", "Failed to delete transaction group");

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Group",
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        _mockTransactionGroupRepository
            .Setup(r => r.DeleteAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedError);

        // Act
        var result = await _transactionGroupService.DeleteAsync(groupId, userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expectedError);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task Authorization_GetByIdAsync_DifferentUser_ShouldNotExposeGroupExistence()
    {
        // Arrange
        const int groupId = 1;
        const int ownerUserId = 10;
        const int attackerUserId = 20;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Secret Group",
            Description = "Sensitive information",
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.GetByIdAsync(groupId, attackerUserId, CancellationToken.None);

        // Assert
        // Should return NotFound to not leak information about existence
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Authorization_UpdateAsync_DifferentUser_ShouldNotExposeGroupExistence()
    {
        // Arrange
        const int groupId = 1;
        const int ownerUserId = 10;
        const int attackerUserId = 20;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Secret Group",
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.UpdateAsync(groupId, attackerUserId, "Hacked Name", null, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Authorization_DeleteAsync_DifferentUser_ShouldNotExposeGroupExistence()
    {
        // Arrange
        const int groupId = 1;
        const int ownerUserId = 10;
        const int attackerUserId = 20;

        var existingGroup = new TransactionGroup
        {
            Id = groupId,
            Name = "Secret Group",
            UserId = ownerUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _transactionGroupService.DeleteAsync(groupId, attackerUserId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion
}
