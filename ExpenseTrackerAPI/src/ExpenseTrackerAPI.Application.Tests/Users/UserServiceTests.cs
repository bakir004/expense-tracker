using ErrorOr;
using Moq;
using ExpenseTrackerAPI.Application.Users;
using ExpenseTrackerAPI.Application.Users.Data;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.Users;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnUsers_WhenRepositoryReturnsUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com" },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
        };
        _mockRepository
            .Setup(r => r.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _service.GetUsersAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Users.Count);
        Assert.Equal(users, result.Value.Users);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnError_WhenRepositoryReturnsError()
    {
        // Arrange
        var error = Error.Failure("database", "Database error");
        _mockRepository
            .Setup(r => r.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(error);

        // Act
        var result = await _service.GetUsersAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
        _mockRepository
            .Setup(r => r.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.Name, result.Value.Name);
        Assert.Equal(user.Email, result.Value.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetUserByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _service.GetUserByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnUser_WhenValidInput()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "password123";
        var createdUser = new User
        {
            Id = 1,
            Name = name,
            Email = email,
            PasswordHash = "hashed_password"
        };

        _mockRepository
            .Setup(r => r.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("user", "User not found"));

        _mockRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _service.CreateUserAsync(name, email, password, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(email, result.Value.Email);
        _mockRepository.Verify(r => r.CreateUserAsync(
            It.Is<User>(u => u.Name == name && u.Email == email && !string.IsNullOrEmpty(u.PasswordHash)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnValidationError_WhenNameIsInvalid()
    {
        // Arrange
        var name = "";
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = await _service.CreateUserAsync(name, email, password, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.InvalidName, result.Errors);
        _mockRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnValidationError_WhenEmailIsInvalid()
    {
        // Arrange
        var name = "John Doe";
        var email = "invalid-email";
        var password = "password123";

        // Act
        var result = await _service.CreateUserAsync(name, email, password, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.InvalidEmail, result.Errors);
        _mockRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnValidationError_WhenPasswordIsTooShort()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "12345"; // Less than 6 characters

        // Act
        var result = await _service.CreateUserAsync(name, email, password, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.InvalidPassword, result.Errors);
        _mockRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnDuplicateEmailError_WhenEmailAlreadyExists()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "password123";
        var existingUser = new User { Id = 1, Email = email };

        _mockRepository
            .Setup(r => r.GetUserByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _service.CreateUserAsync(name, email, password, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.DuplicateEmail, result.Errors);
        _mockRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserBalanceAsync_ShouldReturnBalance_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var balance = (InitialBalance: 1000m, CumulativeDelta: 500m, CurrentBalance: 1500m);
        _mockRepository
            .Setup(r => r.GetUserBalanceAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(balance);

        // Act
        var result = await _service.GetUserBalanceAsync(userId, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(balance, result.Value);
    }

    [Fact]
    public async Task SetInitialBalanceAsync_ShouldReturnUser_WhenValid()
    {
        // Arrange
        var userId = 1;
        var initialBalance = 1000m;
        var user = new User { Id = userId, InitialBalance = initialBalance };

        _mockRepository
            .Setup(r => r.SetInitialBalanceAsync(userId, initialBalance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.SetInitialBalanceAsync(userId, initialBalance, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.InitialBalance, result.Value.InitialBalance);
    }
}

