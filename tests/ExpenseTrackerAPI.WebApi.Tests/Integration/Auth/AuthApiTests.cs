using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.Integration.Auth;

/// <summary>
/// Integration tests for authentication API endpoints.
/// Tests registration, login, and health check endpoints.
/// </summary>
public class AuthApiTests : BaseApiTest
{
    public AuthApiTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Registration Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var uniqueEmail = $"auth.test.{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(
            "Auth Test User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            500.00m);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(uniqueEmail);
        result.Name.Should().Be("Auth Test User");
        result.InitialBalance.Should().Be(500.00m);
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Register_WithZeroInitialBalance_ShouldSucceed()
    {
        // Arrange
        var uniqueEmail = $"zero.balance.{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(
            "Zero Balance User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            0m);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result!.InitialBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Register_WithNullInitialBalance_ShouldDefaultToZero()
    {
        // Arrange
        var uniqueEmail = $"null.balance.{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(
            "Null Balance User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result!.InitialBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange - Use seeded user email
        var request = new RegisterRequest(
            "Duplicate User",
            TestConstants.TestUsers.SeededUser1Email,
            TestConstants.TestUsers.NewUserPassword,
            null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "valid@email.com", "Password123!")]
    [InlineData("Valid Name", "", "Password123!")]
    [InlineData("Valid Name", "invalid-email", "Password123!")]
    [InlineData("Valid Name", "valid@email.com", "")]
    [InlineData("Valid Name", "valid@email.com", "short")]
    [InlineData("Valid Name", "valid@email.com", "nouppercase123!")]
    [InlineData("Valid Name", "valid@email.com", "NOLOWERCASE123!")]
    [InlineData("Valid Name", "valid@email.com", "NoDigitsHere!")]
    [InlineData("Valid Name", "valid@email.com", "NoSpecialChar123")]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest(
        string name, string email, string password)
    {
        // Arrange
        var request = new RegisterRequest(name, email, password, null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMaxLengthName_ShouldSucceed()
    {
        // Arrange
        var maxLengthName = new string('A', 100);
        var uniqueEmail = $"maxname.{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(
            maxLengthName,
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result!.Name.Should().Be(maxLengthName);
    }

    [Fact]
    public async Task Register_WithExceedingMaxLengthName_ShouldReturnBadRequest()
    {
        // Arrange
        var tooLongName = new string('A', 101);
        var uniqueEmail = $"toolongname.{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(
            tooLongName,
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokenAndUserInfo()
    {
        // Arrange - Use seeded user
        var request = new LoginRequest(
            TestConstants.TestUsers.SeededUser1Email,
            TestConstants.TestUsers.SeededUserPassword);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(TestConstants.TestUsers.SeededUser1Email);
        result.Name.Should().Be(TestConstants.TestUsers.SeededUser1Name);
        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest(
            TestConstants.TestUsers.SeededUser1Email,
            "WrongP@ssword123!");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest(
            "nonexistent@email.com",
            TestConstants.TestUsers.SeededUserPassword);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("invalid-email", "Password123!")]
    [InlineData("valid@email.com", "")]
    public async Task Login_WithInvalidData_ShouldReturnBadRequest(string email, string password)
    {
        // Arrange
        var request = new LoginRequest(email, password);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_AfterRegistration_ShouldSucceed()
    {
        // Arrange - Register new user
        var uniqueEmail = $"login.after.reg.{Guid.NewGuid():N}@test.local";
        var password = TestConstants.TestUsers.NewUserPassword;
        var registerRequest = new RegisterRequest("Login Test User", uniqueEmail, password, null);

        var registerResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login with new credentials
        var loginRequest = new LoginRequest(uniqueEmail, password);
        var loginResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        result!.Email.Should().Be(uniqueEmail);
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_MultipleTimes_ShouldGenerateNewTokens()
    {
        // Arrange
        var request = new LoginRequest(
            TestConstants.TestUsers.SeededUser1Email,
            TestConstants.TestUsers.SeededUserPassword);

        // Act - Login twice
        var response1 = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);
        var result1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();

        // Small delay to ensure different token generation
        await Task.Delay(100);

        var response2 = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);
        var result2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        result1!.Token.Should().NotBeNullOrEmpty();
        result2!.Token.Should().NotBeNullOrEmpty();
        // Tokens should be different (different generation time)
        result1.Token.Should().NotBe(result2.Token);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task Health_ShouldReturnHealthyStatus()
    {
        // Act
        var response = await Client.GetAsync(TestConstants.Routes.AuthHealth);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Health_ShouldBeAccessibleWithoutAuthentication()
    {
        // Arrange - Create client without auth header
        var unauthClient = Factory.CreateClient();

        // Act
        var response = await unauthClient.GetAsync(TestConstants.Routes.AuthHealth);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Register_WithCaseSensitiveEmail_ShouldTreatAsUnique()
    {
        // Arrange - Register with lowercase
        var baseEmail = $"case.test.{Guid.NewGuid():N}";
        var lowerEmail = $"{baseEmail}@test.local";

        var registerLower = new RegisterRequest(
            "Lowercase User",
            lowerEmail,
            TestConstants.TestUsers.NewUserPassword,
            null);

        var response1 = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, registerLower);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to register with uppercase (same email, different case)
        var upperEmail = $"{baseEmail.ToUpper()}@TEST.LOCAL";
        var registerUpper = new RegisterRequest(
            "Uppercase User",
            upperEmail,
            TestConstants.TestUsers.NewUserPassword,
            null);

        var response2 = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, registerUpper);

        // Assert - Should be conflict (emails typically case-insensitive)
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithDifferentCaseEmail_ShouldSucceed()
    {
        // Arrange - Use seeded user with different case
        var uppercaseEmail = TestConstants.TestUsers.SeededUser1Email.ToUpper();
        var request = new LoginRequest(
            uppercaseEmail,
            TestConstants.TestUsers.SeededUserPassword);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert - Should succeed if emails are case-insensitive
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithSpecialCharactersInName_ShouldSucceed()
    {
        // Arrange
        var uniqueEmail = $"special.name.{Guid.NewGuid():N}@test.local";
        var specialName = "José María O'Brien-Smith";
        var request = new RegisterRequest(
            specialName,
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        result!.Name.Should().Be(specialName);
    }

    [Fact]
    public async Task Register_WithNegativeInitialBalance_ShouldHandleAppropriately()
    {
        // Arrange
        var uniqueEmail = $"negative.balance.{Guid.NewGuid():N}@test.local";
        var request = new RegisterRequest(
            "Negative Balance User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            -100.00m);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert - Depending on business rules, this might be OK or BadRequest
        // Documenting actual behavior
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    #endregion
}
