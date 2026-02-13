using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Tests.Fixtures;
using ExpenseTrackerAPI.Infrastructure.TransactionGroups;
using ExpenseTrackerAPI.Infrastructure.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerAPI.Infrastructure.Tests.Repositories;

[Collection(nameof(DatabaseCollection))]
public class TransactionGroupRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public TransactionGroupRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<User> CreateTestUserAsync(string? email = null)
    {
        var userRepo = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Test User",
            email: email ?? $"test-{Guid.NewGuid():N}@integration.test",
            passwordHash: "hashed_password",
            initialBalance: 0
        );
        var result = await userRepo.CreateAsync(user, CancellationToken.None);
        Assert.False(result.IsError);
        return result.Value;
    }

    private TransactionGroup CreateTransactionGroup(int userId, string name, string? description = null)
    {
        return new TransactionGroup
        {
            Name = name,
            Description = description,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingGroup_ReturnsGroup()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Vacation Trip", "Summer vacation expenses");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetByIdAsync(groupId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(groupId);
        result.Value.Name.Should().Be("Vacation Trip");
        result.Value.Description.Should().Be("Summer vacation expenses");
        result.Value.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentGroup_ReturnsNotFoundError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);

        // Act
        var result = await sut.GetByIdAsync(99999, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("TransactionGroup");
        result.FirstError.Type.Should().Be(ErrorOr.ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Test Group");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetByIdAsync(groupId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var trackedEntities = _fixture.DbContext.ChangeTracker.Entries<TransactionGroup>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_ExistingUserWithGroups_ReturnsGroups()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        _fixture.DbContext.TransactionGroups.AddRange(
            CreateTransactionGroup(user.Id, "Group A"),
            CreateTransactionGroup(user.Id, "Group B"),
            CreateTransactionGroup(user.Id, "Group C")
        );
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByUserIdAsync_UserWithNoGroups_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        // Act
        var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsGroupsSortedByName()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        // Add groups in non-alphabetical order
        _fixture.DbContext.TransactionGroups.AddRange(
            CreateTransactionGroup(user.Id, "Zebra Project"),
            CreateTransactionGroup(user.Id, "Alpha Project"),
            CreateTransactionGroup(user.Id, "Mango Budget")
        );
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        result.Value[0].Name.Should().Be("Alpha Project");
        result.Value[1].Name.Should().Be("Mango Budget");
        result.Value[2].Name.Should().Be("Zebra Project");
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyUserGroups_NotOtherUsersGroups()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user1 = await CreateTestUserAsync();
        var user2 = await CreateTestUserAsync();

        _fixture.DbContext.TransactionGroups.AddRange(
            CreateTransactionGroup(user1.Id, "User1 Group A"),
            CreateTransactionGroup(user1.Id, "User1 Group B"),
            CreateTransactionGroup(user2.Id, "User2 Group A")
        );
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.GetByUserIdAsync(user1.Id, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(g => g.UserId == user1.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_NonExistentUser_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);

        // Act
        var result = await sut.GetByUserIdAsync(99999, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidGroup_CreatesAndReturnsGroupWithId()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "New Project", "Project description");

        // Act
        var result = await sut.CreateAsync(group, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.Name.Should().Be("New Project");
        result.Value.Description.Should().Be("Project description");
        result.Value.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidGroup_PersistsToDatabase()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Persistent Group");

        // Act
        var createResult = await sut.CreateAsync(group, CancellationToken.None);
        var groupId = createResult.Value.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify by fetching from database
        var fetchResult = await sut.GetByIdAsync(groupId, CancellationToken.None);

        // Assert
        fetchResult.IsError.Should().BeFalse();
        fetchResult.Value.Name.Should().Be("Persistent Group");
    }

    [Fact]
    public async Task CreateAsync_MultipleGroups_AssignsUniqueIds()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group1 = CreateTransactionGroup(user.Id, "Group 1");
        var group2 = CreateTransactionGroup(user.Id, "Group 2");
        var group3 = CreateTransactionGroup(user.Id, "Group 3");

        // Act
        var result1 = await sut.CreateAsync(group1, CancellationToken.None);
        var result2 = await sut.CreateAsync(group2, CancellationToken.None);
        var result3 = await sut.CreateAsync(group3, CancellationToken.None);

        // Assert
        result1.IsError.Should().BeFalse();
        result2.IsError.Should().BeFalse();
        result3.IsError.Should().BeFalse();

        var ids = new[] { result1.Value.Id, result2.Value.Id, result3.Value.Id };
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_CreatesGroupSuccessfully()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "No Description Group", null);

        // Act
        var result = await sut.CreateAsync(group, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentUser_ReturnsError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);

        var group = CreateTransactionGroup(99999, "Orphan Group");

        // Act
        var result = await sut.CreateAsync(group, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingGroup_UpdatesAndReturnsGroup()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Original Name", "Original Description");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;

        // Detach the entity so we can update it
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var existingGroup = await _fixture.DbContext.TransactionGroups.FirstAsync(g => g.Id == groupId);
        existingGroup.Name = "Updated Name";
        existingGroup.Description = "Updated Description";

        var result = await sut.UpdateAsync(existingGroup, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateAsync_ExistingGroup_PersistsChangesToDatabase()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Original Name");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Update the group
        var existingGroup = await _fixture.DbContext.TransactionGroups.FirstAsync(g => g.Id == groupId);
        existingGroup.Name = "Persisted Update";
        await sut.UpdateAsync(existingGroup, CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - Fetch fresh from database
        var fetchResult = await sut.GetByIdAsync(groupId, CancellationToken.None);

        // Assert
        fetchResult.IsError.Should().BeFalse();
        fetchResult.Value.Name.Should().Be("Persisted Update");
    }

    [Fact]
    public async Task UpdateAsync_CanSetDescriptionToNull()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Group With Description", "Has Description");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var existingGroup = await _fixture.DbContext.TransactionGroups.FirstAsync(g => g.Id == groupId);
        existingGroup.Description = null;
        var result = await sut.UpdateAsync(existingGroup, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Description.Should().BeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingGroup_DeletesAndReturnsDeleted()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "To Be Deleted");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await sut.DeleteAsync(groupId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingGroup_RemovesFromDatabase()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group = CreateTransactionGroup(user.Id, "Will Be Gone");
        _fixture.DbContext.TransactionGroups.Add(group);
        await _fixture.DbContext.SaveChangesAsync();
        var groupId = group.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        await sut.DeleteAsync(groupId, CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify by trying to fetch
        var fetchResult = await sut.GetByIdAsync(groupId, CancellationToken.None);

        // Assert
        fetchResult.IsError.Should().BeTrue();
        fetchResult.FirstError.Type.Should().Be(ErrorOr.ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentGroup_ReturnsNotFoundError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);

        // Act
        var result = await sut.DeleteAsync(99999, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorOr.ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_OnlyDeletesSpecifiedGroup()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        var group1 = CreateTransactionGroup(user.Id, "Group 1");
        var group2 = CreateTransactionGroup(user.Id, "Group 2");
        var group3 = CreateTransactionGroup(user.Id, "Group 3");

        _fixture.DbContext.TransactionGroups.AddRange(group1, group2, group3);
        await _fixture.DbContext.SaveChangesAsync();

        var groupToDeleteId = group2.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        await sut.DeleteAsync(groupToDeleteId, CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify remaining groups
        var remainingGroups = await sut.GetByUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        remainingGroups.IsError.Should().BeFalse();
        remainingGroups.Value.Should().HaveCount(2);
        remainingGroups.Value.Should().NotContain(g => g.Id == groupToDeleteId);
    }

    #endregion

    #region Integration / Interaction Tests

    [Fact]
    public async Task CreateUpdateDelete_FullLifecycle_WorksCorrectly()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);
        var user = await CreateTestUserAsync();

        // Create
        var group = CreateTransactionGroup(user.Id, "Lifecycle Test", "Initial Description");
        var createResult = await sut.CreateAsync(group, CancellationToken.None);
        createResult.IsError.Should().BeFalse();
        var groupId = createResult.Value.Id;
        _fixture.DbContext.ChangeTracker.Clear();

        // Update
        var toUpdate = await _fixture.DbContext.TransactionGroups.FirstAsync(g => g.Id == groupId);
        toUpdate.Name = "Updated Lifecycle Test";
        toUpdate.Description = "Updated Description";
        var updateResult = await sut.UpdateAsync(toUpdate, CancellationToken.None);
        updateResult.IsError.Should().BeFalse();
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify update
        var verifyResult = await sut.GetByIdAsync(groupId, CancellationToken.None);
        verifyResult.IsError.Should().BeFalse();
        verifyResult.Value.Name.Should().Be("Updated Lifecycle Test");
        verifyResult.Value.Description.Should().Be("Updated Description");

        // Delete
        var deleteResult = await sut.DeleteAsync(groupId, CancellationToken.None);
        deleteResult.IsError.Should().BeFalse();
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify deletion
        var finalResult = await sut.GetByIdAsync(groupId, CancellationToken.None);
        finalResult.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleUsers_IndependentTransactionGroups()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionGroupRepository(_fixture.DbContext);

        var user1 = await CreateTestUserAsync();
        var user2 = await CreateTestUserAsync();

        // Create groups for both users
        await sut.CreateAsync(CreateTransactionGroup(user1.Id, "User1 Project"), CancellationToken.None);
        await sut.CreateAsync(CreateTransactionGroup(user2.Id, "User2 Project A"), CancellationToken.None);
        await sut.CreateAsync(CreateTransactionGroup(user2.Id, "User2 Project B"), CancellationToken.None);
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var user1Groups = await sut.GetByUserIdAsync(user1.Id, CancellationToken.None);
        var user2Groups = await sut.GetByUserIdAsync(user2.Id, CancellationToken.None);

        // Assert
        user1Groups.IsError.Should().BeFalse();
        user2Groups.IsError.Should().BeFalse();

        user1Groups.Value.Should().HaveCount(1);
        user2Groups.Value.Should().HaveCount(2);

        user1Groups.Value.Should().OnlyContain(g => g.UserId == user1.Id);
        user2Groups.Value.Should().OnlyContain(g => g.UserId == user2.Id);
    }

    #endregion
}
