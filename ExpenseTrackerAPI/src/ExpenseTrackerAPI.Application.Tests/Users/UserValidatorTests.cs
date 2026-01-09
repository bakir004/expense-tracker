using ExpenseTrackerAPI.Application.Users;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.Users;

public class UserValidatorTests
{
    [Fact]
    public void ValidateCreateUserRequest_ValidInput_ShouldReturnSuccess()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateUserRequest_EmptyName_ShouldReturnInvalidName()
    {
        // Arrange
        var name = "";
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_WhitespaceName_ShouldReturnInvalidName()
    {
        // Arrange
        var name = "   ";
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_NullName_ShouldReturnInvalidName()
    {
        // Arrange
        string? name = null;
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name!, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_NameTooLong_ShouldReturnInvalidName()
    {
        // Arrange
        var name = new string('A', 101); // 101 characters (exceeds 100 limit)
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidName, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_NameAtMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var name = new string('A', 100); // Exactly 100 characters
        var email = "john@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateUserRequest_EmptyEmail_ShouldReturnInvalidEmail()
    {
        // Arrange
        var name = "John Doe";
        var email = "";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidEmail, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_NullEmail_ShouldReturnInvalidEmail()
    {
        // Arrange
        var name = "John Doe";
        string? email = null;
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email!, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidEmail, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_InvalidEmailFormat_ShouldReturnInvalidEmail()
    {
        // Arrange
        var name = "John Doe";
        var email = "notanemail";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidEmail, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_EmailWithSpaces_ShouldReturnInvalidEmail()
    {
        // Arrange
        var name = "John Doe";
        var email = "john @example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidEmail, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_EmailWithoutAtSymbol_ShouldReturnInvalidEmail()
    {
        // Arrange
        var name = "John Doe";
        var email = "johnexample.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidEmail, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_ValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var name = "John Doe";
        var email = "john.doe@example.com";
        var password = "password123";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.False(result.IsError);
    }

    [Fact]
    public void ValidateCreateUserRequest_EmptyPassword_ShouldReturnInvalidPassword()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "";

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidPassword, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_NullPassword_ShouldReturnInvalidPassword()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        string? password = null;

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password!);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidPassword, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_PasswordTooShort_ShouldReturnInvalidPassword()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "12345"; // 5 characters (less than 6)

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(UserErrors.InvalidPassword, result.FirstError);
    }

    [Fact]
    public void ValidateCreateUserRequest_PasswordAtMinLength_ShouldReturnSuccess()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var password = "123456"; // Exactly 6 characters

        // Act
        var result = UserValidator.ValidateCreateUserRequest(name, email, password);

        // Assert
        Assert.False(result.IsError);
    }
}

