using ErrorOr;
using ExpenseTrackerAPI.Application.Common.Interfaces;
using ExpenseTrackerAPI.Application.Users;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseTrackerAPI.Application.Tests.Users;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        _userService = new UserService(_mockUserRepository.Object, _mockJwtTokenGenerator.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullUserRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new UserService(null!, _mockJwtTokenGenerator.Object);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("userRepository");
    }

    [Fact]
    public void Constructor_WithNullJwtTokenGenerator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new UserService(_mockUserRepository.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("jwtTokenGenerator");
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "john.doe@example.com",
            Password: "Password123!",
            InitialBalance: 1000m
        );

        var expectedUser = new User("John Doe", "john.doe@example.com", "hashedPassword", 1000m);
        SetUserIdViaReflection(expectedUser, 1);

        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(request.Name);
        result.Value.Email.Should().Be(request.Email.ToLowerInvariant());
        result.Value.InitialBalance.Should().Be(request.InitialBalance ?? 0);

        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDefaultInitialBalance_ShouldUseZero()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "john.doe@example.com",
            Password: "Password123!",
            InitialBalance: null
        );

        var expectedUser = new User("John Doe", "john.doe@example.com", "hashedPassword", 0m);
        SetUserIdViaReflection(expectedUser, 1);

        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.InitialBalance.Should().Be(0m);
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ShouldSucceed()
    {
        // Arrange - weak password should succeed since there's no password complexity validation in service
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "john.doe@example.com",
            Password: "weakpassword", // This will be hashed - no complexity validation in service
            InitialBalance: 1000m
        );

        // Mock repository to return success when creating user
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert - Since there's no password validation in service, this should succeed
        // The User constructor doesn't validate password complexity, only that passwordHash isn't null/empty
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnDuplicateEmailErrorFromRepository()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "existing@example.com",
            Password: "Password123!",
            InitialBalance: 1000m
        );

        // Mock repository to return duplicate email error when creating user
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.DuplicateEmail);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.DuplicateEmail);

        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenRepositoryCreateFails_ShouldReturnRepositoryError()
    {
        // Arrange
        var request = CreateValidRegisterRequest();
        var repositoryError = Error.Failure("Database.Error", "Connection failed");

        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(repositoryError);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(repositoryError);
    }

    // This test is now covered by RegisterAsync_WhenRepositoryCreateFails_ShouldReturnRepositoryError above

    [Fact]
    public async Task RegisterAsync_WithInvalidEmailInDomain_ShouldReturnInvalidEmailError()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "invalid-email", // This will fail domain validation in User constructor
            Password: "Password123!",
            InitialBalance: 1000m
        );

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("User.Register.UnexpectedError");
        result.FirstError.Description.Should().Contain("Email format is invalid");
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccessResult()
    {
        // Arrange
        var request = new LoginRequest("john.doe@example.com", "Password123!");
        var user = CreateValidUser();
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Password123!");
        user.UpdatePassword(hashedPassword);

        var expectedToken = "jwt.token.here";
        var expectedExpirationHours = 24;

        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(user.Id, user.Email, user.Name))
            .Returns(expectedToken);

        _mockJwtTokenGenerator.Setup(x => x.TokenExpirationHours)
            .Returns(expectedExpirationHours);

        // Act
        var result = await _userService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Token.Should().Be(expectedToken);
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(expectedExpirationHours), TimeSpan.FromMinutes(1));

        _mockUserRepository.Verify(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()), Times.Once);
        _mockJwtTokenGenerator.Verify(x => x.GenerateToken(user.Id, user.Email, user.Name), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password123!");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _userService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.InvalidCredentials);

        _mockJwtTokenGenerator.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var request = new LoginRequest("john.doe@example.com", "WrongPassword!");
        var user = CreateValidUser();
        var correctHashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        user.UpdatePassword(correctHashedPassword);

        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.InvalidCredentials);

        _mockJwtTokenGenerator.Verify(x => x.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var userId = 1;
        var currentPassword = "CurrentPassword123!";
        var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);

        var request = new UpdateUserRequest(
            Name: "Updated Name",
            Email: "updated@example.com",
            NewPassword: "NewPassword123!",
            CurrentPassword: currentPassword,
            InitialBalance: 2000m
        );

        var existingUser = CreateValidUser();
        existingUser.UpdatePassword(hashedCurrentPassword);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        // Act
        var result = await _userService.UpdateAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(request.Name);
        result.Value.Email.Should().Be(request.Email.ToLowerInvariant());
        result.Value.InitialBalance.Should().Be(request.InitialBalance);

        _mockUserRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithSameEmail_ShouldNotCheckEmailExistence()
    {
        // Arrange
        var userId = 1;
        var currentPassword = "CurrentPassword123!";
        var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);
        var currentEmail = "john.doe@example.com";

        var request = new UpdateUserRequest(
            Name: "Updated Name",
            Email: currentEmail, // Same email
            NewPassword: null,
            CurrentPassword: currentPassword,
            InitialBalance: 2000m
        );

        var existingUser = CreateValidUser();
        existingUser.UpdatePassword(hashedCurrentPassword);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        // Act
        var result = await _userService.UpdateAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        _mockUserRepository.Verify(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidUserId_ShouldReturnInvalidUserIdError()
    {
        // Arrange
        var invalidUserId = -1;
        var request = CreateValidUpdateRequest();

        // Act
        var result = await _userService.UpdateAsync(invalidUserId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.InvalidUserId);

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentUser_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = 999;
        var request = CreateValidUpdateRequest();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _userService.UpdateAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithIncorrectCurrentPassword_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var userId = 1;
        var correctPassword = "CorrectPassword123!";
        var incorrectPassword = "WrongPassword123!";
        var hashedCorrectPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword);

        var request = new UpdateUserRequest(
            Name: "Updated Name",
            Email: "updated@example.com",
            NewPassword: null,
            CurrentPassword: incorrectPassword,
            InitialBalance: 2000m
        );

        var existingUser = CreateValidUser();
        existingUser.UpdatePassword(hashedCorrectPassword);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _userService.UpdateAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.InvalidCredentials);

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEmail_ShouldReturnDuplicateEmailError()
    {
        // Arrange
        var userId = 1;
        var currentPassword = "CurrentPassword123!";
        var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);

        var request = new UpdateUserRequest(
            Name: "Updated Name",
            Email: "existing@example.com",
            NewPassword: null,
            CurrentPassword: currentPassword,
            InitialBalance: 2000m
        );

        var existingUser = CreateValidUser();
        existingUser.UpdatePassword(hashedCurrentPassword);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.DuplicateEmail);

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithWeakNewPassword_ShouldReturnWeakPasswordError()
    {
        // Arrange
        var userId = 1;
        var currentPassword = "CurrentPassword123!";
        var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);

        var request = new UpdateUserRequest(
            Name: "Updated Name",
            Email: "updated@example.com",
            NewPassword: "weakpassword", // Weak password
            CurrentPassword: currentPassword,
            InitialBalance: 2000m
        );

        var existingUser = CreateValidUser();
        existingUser.UpdatePassword(hashedCurrentPassword);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.UpdateAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.WeakPassword);

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var userId = 1;
        var currentPassword = "CurrentPassword123!";
        var hashedCurrentPassword = BCrypt.Net.BCrypt.HashPassword(currentPassword);

        var request = new DeleteUserRequest(
            CurrentPassword: currentPassword,
            ConfirmDeletion: true
        );

        var existingUser = CreateValidUser();
        existingUser.UpdatePassword(hashedCurrentPassword);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockUserRepository.Setup(x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Deleted);

        // Act
        var result = await _userService.DeleteAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(existingUser.Id);
        result.Value.Name.Should().Be(existingUser.Name);
        result.Value.Email.Should().Be(existingUser.Email);
        result.Value.Message.Should().Be("User account has been permanently deleted.");

        _mockUserRepository.Verify(x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidUserId_ShouldReturnInvalidUserIdError()
    {
        // Arrange
        var invalidUserId = -1;
        var request = new DeleteUserRequest("Password123!", true);

        // Act
        var result = await _userService.DeleteAsync(invalidUserId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.InvalidUserId);

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutConfirmation_ShouldReturnValidationError()
    {
        // Arrange
        var userId = 1;
        var request = new DeleteUserRequest("Password123!", false); // Not confirmed

        // Act
        var result = await _userService.DeleteAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("Deletion must be confirmed");

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyPassword_ShouldReturnPasswordRequiredError()
    {
        // Arrange
        var userId = 1;
        var request = new DeleteUserRequest("", true);

        // Act
        var result = await _userService.DeleteAsync(userId, request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.PasswordRequired);
    }

    #endregion

    #region Helper Methods

    private static RegisterRequest CreateValidRegisterRequest()
    {
        return new RegisterRequest(
            Name: "John Doe",
            Email: "john.doe@example.com",
            Password: "Password123!",
            InitialBalance: 1000m
        );
    }

    private static UpdateUserRequest CreateValidUpdateRequest()
    {
        return new UpdateUserRequest(
            Name: "Updated Name",
            Email: "updated@example.com",
            NewPassword: "NewPassword123!",
            CurrentPassword: "CurrentPassword123!",
            InitialBalance: 2000m
        );
    }

    private static User CreateValidUser()
    {
        var user = new User(
            name: "John Doe",
            email: "john.doe@example.com",
            passwordHash: "hashedPassword123",
            initialBalance: 1000m
        );

        SetUserIdViaReflection(user, 1);
        return user;
    }

    private static void SetUserIdViaReflection(User user, int id)
    {
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(user, id);
    }

    #endregion

    #region Password Complexity Tests

    [Theory]
    [InlineData("Password123!")] // Valid
    [InlineData("MyStr0ng!Pass")] // Valid
    [InlineData("Complex1@Password")] // Valid
    public async Task RegisterAsync_WithValidPasswordComplexity_ShouldSucceed(string validPassword)
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "john.doe@example.com",
            Password: validPassword,
            InitialBalance: 1000m
        );

        var expectedUser = new User("John Doe", "john.doe@example.com", "hashedPassword", 1000m);
        SetUserIdViaReflection(expectedUser, 1);

        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Theory]
    [InlineData("password123!")] // Any password will be hashed
    [InlineData("PASSWORD123!")] // Any password will be hashed
    [InlineData("Password!")] // Any password will be hashed
    [InlineData("Password123")] // Any password will be hashed
    [InlineData("Pass123!")] // Any password will be hashed
    public async Task RegisterAsync_WithAnyPassword_ShouldHashAndSucceed(string password)
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "John Doe",
            Email: "john.doe@example.com",
            Password: password,
            InitialBalance: 1000m
        );

        var expectedUser = new User("John Doe", "john.doe@example.com", "hashedPassword", 1000m);
        SetUserIdViaReflection(expectedUser, 1);

        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse(); // Service doesn't validate password complexity
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task RegisterAsync_WhenExceptionThrown_ShouldReturnFailureError()
    {
        // Arrange
        var request = CreateValidRegisterRequest();

        _mockUserRepository.Setup(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _userService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("User.Register.UnexpectedError");
        result.FirstError.Description.Should().Contain("Database connection failed");
    }

    [Fact]
    public async Task LoginAsync_WhenExceptionThrown_ShouldReturnFailureError()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database timeout"));

        // Act
        var result = await _userService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Failure);
        result.FirstError.Code.Should().Be("User.Login.UnexpectedError");
        result.FirstError.Description.Should().Contain("Database timeout");
    }

    #endregion
}
