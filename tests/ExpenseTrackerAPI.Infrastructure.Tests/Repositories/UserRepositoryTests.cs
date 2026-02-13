using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Tests.Fixtures;
using ExpenseTrackerAPI.Infrastructure.Users;
using FluentAssertions;

namespace ExpenseTrackerAPI.Infrastructure.Tests.Repositories;

[Collection(nameof(DatabaseCollection))]
public class UserRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithValidUser_ShouldCreateUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "John Doe",
            email: "john.doe@example.com",
            passwordHash: "hashed_password",
            initialBalance: 0
        );

        // Act
        var result = await sut.CreateAsync(user, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.Name.Should().Be("John Doe");
        result.Value.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Jane Smith",
            email: "jane.smith@example.com",
            passwordHash: "hashed_password",
            initialBalance: 0
        );
        var createResult = await sut.CreateAsync(user, CancellationToken.None);
        var userId = createResult.Value.Id;

        // Act
        var result = await sut.GetByIdAsync(userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(userId);
        result.Value.Name.Should().Be("Jane Smith");
        result.Value.Email.Should().Be("jane.smith@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var nonExistentId = 99999;

        // Act
        var result = await sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User");
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Bob Johnson",
            email: "bob.johnson@example.com",
            passwordHash: "hashed_password",
            initialBalance: 0
        );
        await sut.CreateAsync(user, CancellationToken.None);

        // Act
        var result = await sut.GetByEmailAsync("bob.johnson@example.com", CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("bob.johnson@example.com");
        result.Value.Name.Should().Be("Bob Johnson");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ShouldReturnError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);

        // Act
        var result = await sut.GetByEmailAsync("nonexistent@example.com", CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User");
    }

    [Fact]
    public async Task ExistsByEmailAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Alice Williams",
            email: "alice.williams@example.com",
            passwordHash: "hashed_password",
            initialBalance: 0
        );
        await sut.CreateAsync(user, CancellationToken.None);

        // Act
        var result = await sut.ExistsByEmailAsync("alice.williams@example.com", CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_WithNonExistentEmail_ShouldReturnFalse()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);

        // Act
        var result = await sut.ExistsByEmailAsync("nonexistent@example.com", CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleUsers_ShouldReturnAllUsers()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var users = new[]
        {
            new User(
                name: "User One",
                email: "user1@example.com",
                passwordHash: "hash1",
                initialBalance: 0
            ),
            new User(
                name: "User Two",
                email: "user2@example.com",
                passwordHash: "hash2",
                initialBalance: 100
            ),
            new User(
                name: "User Three",
                email: "user3@example.com",
                passwordHash: "hash3",
                initialBalance: 50
            )
        };

        foreach (var user in users)
        {
            await sut.CreateAsync(user, CancellationToken.None);
        }

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(u => u.Email == "user1@example.com");
        result.Value.Should().Contain(u => u.Email == "user2@example.com");
        result.Value.Should().Contain(u => u.Email == "user3@example.com");
    }

    [Fact]
    public async Task GetAllAsync_WithNoUsers_ShouldReturnEmptyList()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);

        // Act
        var result = await sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithValidUser_ShouldUpdateUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Original Name",
            email: "original@example.com",
            passwordHash: "original_hash",
            initialBalance: 0
        );
        var createResult = await sut.CreateAsync(user, CancellationToken.None);
        var createdUser = createResult.Value;

        // Modify the user
        createdUser.UpdateProfile("Updated Name", "updated@example.com");

        // Act
        var result = await sut.UpdateAsync(createdUser, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(createdUser.Id);
        result.Value.Name.Should().Be("Updated Name");
        result.Value.Email.Should().Be("updated@example.com");

        // Verify persistence
        var getResult = await sut.GetByIdAsync(createdUser.Id, CancellationToken.None);
        getResult.Value.Name.Should().Be("Updated Name");
        getResult.Value.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Non Existent",
            email: "nonexistent@example.com",
            passwordHash: "hash",
            initialBalance: 0
        );

        // We need to somehow set the Id to a non-existent value
        // Since User has private setter for Id, we'll create and delete a user
        var createResult = await sut.CreateAsync(user, CancellationToken.None);
        createResult.IsError.Should().BeFalse();
        var userId = createResult.Value.Id;
        await sut.DeleteAsync(userId, CancellationToken.None);

        // Now try to update the deleted user
        var result = await sut.UpdateAsync(createResult.Value, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingUser_ShouldDeleteUser()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "To Be Deleted",
            email: "delete.me@example.com",
            passwordHash: "hash",
            initialBalance: 0
        );
        var createResult = await sut.CreateAsync(user, CancellationToken.None);
        createResult.IsError.Should().BeFalse();
        var userId = createResult.Value.Id;

        // Act
        var result = await sut.DeleteAsync(userId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        // Verify user is deleted
        var getResult = await sut.GetByIdAsync(userId, CancellationToken.None);
        getResult.IsError.Should().BeTrue();
        getResult.FirstError.Code.Should().Be("User");
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var nonExistentId = 99999;

        // Act
        var result = await sut.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user1 = new User(
            name: "First User",
            email: "duplicate@example.com",
            passwordHash: "hash1",
            initialBalance: 0
        );
        await sut.CreateAsync(user1, CancellationToken.None);

        var user2 = new User(
            name: "Second User",
            email: "duplicate@example.com", // Same email
            passwordHash: "hash2",
            initialBalance: 0
        );

        // Act
        var result = await sut.CreateAsync(user2, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Email");
    }

    [Fact]
    public async Task CreateAsync_WithInitialBalance_ShouldCreateUserWithBalance()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "User With Balance",
            email: "balance@example.com",
            passwordHash: "hash",
            initialBalance: 1000.50m
        );

        // Act
        var result = await sut.CreateAsync(user, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.InitialBalance.Should().Be(1000.50m);

        // Verify persistence
        var getResult = await sut.GetByIdAsync(result.Value.Id, CancellationToken.None);
        getResult.Value.InitialBalance.Should().Be(1000.50m);
    }

    [Fact]
    public async Task UpdateAsync_WithPasswordChange_ShouldUpdatePassword()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Test User",
            email: "test@example.com",
            passwordHash: "original_hash",
            initialBalance: 0
        );
        var createResult = await sut.CreateAsync(user, CancellationToken.None);
        createResult.IsError.Should().BeFalse();
        var createdUser = createResult.Value;

        // Change password
        createdUser.UpdatePassword("new_hash");

        // Act
        var result = await sut.UpdateAsync(createdUser, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PasswordHash.Should().Be("new_hash");

        // Verify persistence
        var getResult = await sut.GetByIdAsync(createdUser.Id, CancellationToken.None);
        getResult.Value.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public async Task UpdateAsync_WithInitialBalanceChange_ShouldUpdateBalance()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Test User",
            email: "test@example.com",
            passwordHash: "hash",
            initialBalance: 100.00m
        );
        var createResult = await sut.CreateAsync(user, CancellationToken.None);
        createResult.IsError.Should().BeFalse();
        var createdUser = createResult.Value;

        // Change initial balance
        createdUser.UpdateInitialBalance(500.00m);

        // Act
        var result = await sut.UpdateAsync(createdUser, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.InitialBalance.Should().Be(500.00m);

        // Verify persistence
        var getResult = await sut.GetByIdAsync(createdUser.Id, CancellationToken.None);
        getResult.Value.InitialBalance.Should().Be(500.00m);
    }
}
