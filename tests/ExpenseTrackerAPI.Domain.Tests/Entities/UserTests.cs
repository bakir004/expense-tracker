using ExpenseTrackerAPI.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

public class UserTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateUser()
    {
        // Arrange
        var name = "John Doe";
        var email = "john.doe@example.com";
        var passwordHash = "hashedPassword123";
        var initialBalance = 1000.50m;

        // Act
        var user = new User(name, email, passwordHash, initialBalance);

        // Assert
        user.Name.Should().Be(name);
        user.Email.Should().Be(email.ToLowerInvariant());
        user.PasswordHash.Should().Be(passwordHash);
        user.InitialBalance.Should().Be(initialBalance);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.Id.Should().Be(0); // Default value for new entity
    }

    [Fact]
    public void Constructor_WithDefaultInitialBalance_ShouldSetToZero()
    {
        // Arrange
        var name = "Jane Doe";
        var email = "jane.doe@example.com";
        var passwordHash = "hashedPassword456";

        // Act
        var user = new User(name, email, passwordHash);

        // Assert
        user.InitialBalance.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithWhitespaceInNameAndEmail_ShouldTrimAndNormalizeEmail()
    {
        // Arrange
        var name = "  John Doe  ";
        var email = "  JOHN.DOE@EXAMPLE.COM  ";
        var passwordHash = "hashedPassword123";

        // Act
        var user = new User(name, email, passwordHash);

        // Assert
        user.Name.Should().Be("John Doe");
        user.Email.Should().Be("john.doe@example.com");
    }

    #endregion

    #region Constructor Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(invalidName, email, passwordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var name = new string('A', 101); // 101 characters
        var email = "test@example.com";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, email, passwordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot exceed 100 characters*")
            .And.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange
        var name = "John Doe";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, invalidEmail, passwordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be empty*")
            .And.ParamName.Should().Be("email");
    }

    [Fact]
    public void Constructor_WithEmailTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "John Doe";
        var email = new string('a', 250) + "@example.com"; // > 254 characters
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, email, passwordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot exceed 254 characters*")
            .And.ParamName.Should().Be("email");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test..test@example.com")]
    [InlineData("test@example")]
    public void Constructor_WithInvalidEmailFormat_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange
        var name = "John Doe";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, invalidEmail, passwordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email format is invalid*")
            .And.ParamName.Should().Be("email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPasswordHash_ShouldThrowArgumentException(string invalidPasswordHash)
    {
        // Arrange
        var name = "John Doe";
        var email = "test@example.com";

        // Act & Assert
        var act = () => new User(name, email, invalidPasswordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password hash cannot be empty*")
            .And.ParamName.Should().Be("passwordHash");
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public void UpdateProfile_WithValidParameters_ShouldUpdateUserAndTimestamp()
    {
        // Arrange
        var user = CreateValidUser();
        var originalUpdatedAt = user.UpdatedAt;
        var newName = "Jane Smith";
        var newEmail = "jane.smith@example.com";

        // Wait a bit to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        user.UpdateProfile(newName, newEmail);

        // Assert
        user.Name.Should().Be(newName);
        user.Email.Should().Be(newEmail.ToLowerInvariant());
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithWhitespaceInNameAndEmail_ShouldTrimAndNormalizeEmail()
    {
        // Arrange
        var user = CreateValidUser();
        var newName = "  Jane Smith  ";
        var newEmail = "  JANE.SMITH@EXAMPLE.COM  ";

        // Act
        user.UpdateProfile(newName, newEmail);

        // Assert
        user.Name.Should().Be("Jane Smith");
        user.Email.Should().Be("jane.smith@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var user = CreateValidUser();
        var newEmail = "test@example.com";

        // Act & Assert
        var act = () => user.UpdateProfile(invalidName, newEmail);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty*")
            .And.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public void UpdateProfile_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
    {
        // Arrange
        var user = CreateValidUser();
        var newName = "Jane Smith";

        // Act & Assert
        var act = () => user.UpdateProfile(newName, invalidEmail);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email format is invalid*")
            .And.ParamName.Should().Be("email");
    }

    #endregion

    #region UpdatePassword Tests

    [Fact]
    public void UpdatePassword_WithValidPasswordHash_ShouldUpdatePasswordAndTimestamp()
    {
        // Arrange
        var user = CreateValidUser();
        var originalUpdatedAt = user.UpdatedAt;
        var newPasswordHash = "newHashedPassword456";

        // Wait a bit to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        user.UpdatePassword(newPasswordHash);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePassword_WithInvalidPasswordHash_ShouldThrowArgumentException(string invalidPasswordHash)
    {
        // Arrange
        var user = CreateValidUser();

        // Act & Assert
        var act = () => user.UpdatePassword(invalidPasswordHash);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password hash cannot be empty*")
            .And.ParamName.Should().Be("passwordHash");
    }

    #endregion

    #region UpdateInitialBalance Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1000.50)]
    [InlineData(-500.25)]
    [InlineData(999999.99)]
    public void UpdateInitialBalance_WithValidBalance_ShouldUpdateBalanceAndTimestamp(decimal newBalance)
    {
        // Arrange
        var user = CreateValidUser();
        var originalUpdatedAt = user.UpdatedAt;

        // Wait a bit to ensure timestamp difference
        Thread.Sleep(10);

        // Act
        user.UpdateInitialBalance(newBalance);

        // Assert
        user.InitialBalance.Should().Be(newBalance);
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region Email Validation Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.com")]
    [InlineData("123456@example.com")]
    [InlineData("test_user@sub.example.com")]
    [InlineData("a@b.co")]
    public void Constructor_WithValidEmailFormats_ShouldNotThrow(string validEmail)
    {
        // Arrange
        var name = "John Doe";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, validEmail, passwordHash);
        act.Should().NotThrow();
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void Constructor_WithNameExactly100Characters_ShouldNotThrow()
    {
        // Arrange
        var name = new string('A', 100); // Exactly 100 characters
        var email = "test@example.com";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, email, passwordHash);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmailExactly254Characters_ShouldNotThrow()
    {
        // Arrange
        var name = "John Doe";
        var localPart = new string('a', 240); // 240 + "@example.com" = 252 chars
        var email = $"{localPart}@example.com";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, email, passwordHash);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-999999999.99)]
    [InlineData(999999999.99)]
    [InlineData(1000000.00)]
    [InlineData(-1000000.00)]
    public void Constructor_WithExtremeInitialBalances_ShouldNotThrow(decimal extremeBalance)
    {
        // Arrange
        var name = "John Doe";
        var email = "test@example.com";
        var passwordHash = "hashedPassword123";

        // Act & Assert
        var act = () => new User(name, email, passwordHash, extremeBalance);
        act.Should().NotThrow();
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void User_PropertiesWithPrivateSetters_ShouldBeImmutableFromOutside()
    {
        // Arrange
        var user = CreateValidUser();

        // Act & Assert
        var idProperty = typeof(User).GetProperty(nameof(User.Id));
        var nameProperty = typeof(User).GetProperty(nameof(User.Name));
        var emailProperty = typeof(User).GetProperty(nameof(User.Email));
        var passwordHashProperty = typeof(User).GetProperty(nameof(User.PasswordHash));
        var initialBalanceProperty = typeof(User).GetProperty(nameof(User.InitialBalance));
        var createdAtProperty = typeof(User).GetProperty(nameof(User.CreatedAt));
        var updatedAtProperty = typeof(User).GetProperty(nameof(User.UpdatedAt));

        idProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
        nameProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
        emailProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
        passwordHashProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
        initialBalanceProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
        createdAtProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
        updatedAtProperty!.SetMethod.Should().NotBeNull().And.Subject.IsPrivate.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static User CreateValidUser()
    {
        return new User(
            name: "John Doe",
            email: "john.doe@example.com",
            passwordHash: "hashedPassword123",
            initialBalance: 1000m
        );
    }

    #endregion
}
