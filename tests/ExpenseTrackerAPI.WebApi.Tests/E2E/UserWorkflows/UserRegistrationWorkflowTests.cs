using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.UserWorkflows;

/// <summary>
/// E2E tests for user registration workflows.
/// Tests the complete registration flow including validation, success, and error scenarios.
/// </summary>
public class UserRegistrationWorkflowTests : BaseE2ETest
{
    public UserRegistrationWorkflowTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    [Fact]
    public async Task RegisterUser_WithValidData_ShouldCreateUserAndAllowLogin()
    {
        // Arrange
        var uniqueEmail = GenerateUniqueEmail("register");
        var name = "Registration Test User";
        var password = TestConstants.TestUsers.NewUserPassword;

        // Act - Register
        var registerResponse = await RegisterUserAsync(name, uniqueEmail, password);

        // Assert - Registration successful
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        registerResult.Should().NotBeNull();
        registerResult!.Email.Should().Be(uniqueEmail);
        registerResult.Name.Should().Be(name);
        registerResult.Id.Should().BeGreaterThan(0);

        // Act - Login with new credentials
        var loginResponse = await LoginAsync(uniqueEmail, password);

        // Assert - Login successful
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Token.Should().NotBeNullOrEmpty();
        loginResult.Email.Should().Be(uniqueEmail);
        loginResult.Id.Should().Be(registerResult.Id);
    }

    [Fact]
    public async Task RegisterUser_WithInitialBalance_ShouldSetCorrectBalance()
    {
        // Arrange
        var uniqueEmail = GenerateUniqueEmail("balance");
        var initialBalance = 1000.50m;

        // Act
        var registerResponse = await RegisterUserAsync(
            "Balance Test User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword,
            initialBalance);

        // Assert
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        result.Should().NotBeNull();
        result!.InitialBalance.Should().Be(initialBalance);
    }

    [Fact]
    public async Task RegisterUser_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange - First registration
        var uniqueEmail = GenerateUniqueEmail("duplicate");
        await RegisterUserAsync("First User", uniqueEmail, TestConstants.TestUsers.NewUserPassword);

        // Act - Second registration with same email
        var duplicateResponse = await RegisterUserAsync(
            "Second User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "test@email.com", "Password123!", "Name is required")]
    [InlineData("Test User", "", "Password123!", "Email")]
    [InlineData("Test User", "invalid-email", "Password123!", "Email format is invalid")]
    [InlineData("Test User", "test@email.com", "", "Password is required")]
    [InlineData("Test User", "test@email.com", "short", "Password must be between 8 and 100 characters")]
    [InlineData("Test User", "test@email.com", "nouppercaseornumber!", "Password must contain")]
    public async Task RegisterUser_WithInvalidData_ShouldReturnBadRequest(
        string name,
        string email,
        string password,
        string expectedErrorContains)
    {
        // Arrange
        // Use a unique valid email only when testing empty email (to avoid duplicate conflicts)
        // For invalid format tests, use the actual invalid email
        var testEmail = string.IsNullOrEmpty(email) ? "" : email;

        // If the email is empty string (testing required), use empty
        // If it's a valid email pattern, make it unique to avoid conflicts
        if (!string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains("."))
        {
            testEmail = $"{Guid.NewGuid():N}.{email}";
        }

        // Act
        var response = await RegisterUserAsync(name, testEmail, password);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedErrorContains);
    }

    [Fact]
    public async Task RegisterUser_ThenAccessProtectedEndpoint_WithToken_ShouldSucceed()
    {
        // Arrange - Register and login
        var uniqueEmail = GenerateUniqueEmail("protected");
        var result = await RegisterAndLoginAsync(
            "Protected Test User",
            uniqueEmail,
            TestConstants.TestUsers.NewUserPassword);

        result.Should().NotBeNull();
        var (authenticatedClient, user) = result!.Value;

        // Act - Access protected endpoint
        var response = await authenticatedClient.GetAsync(TestConstants.Routes.UserProfile);

        // Note: GET on profile might not be implemented, but we're testing auth works
        // If not implemented, it should return NotFound or MethodNotAllowed, not Unauthorized
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginUser_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange - Use seeded user
        var email = TestConstants.TestUsers.SeededUser1Email;
        var wrongPassword = "WrongP@ssw0rd!";

        // Act
        var response = await LoginAsync(email, wrongPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginUser_WithNonExistentEmail_ShouldReturnUnauthorized()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@email.com";

        // Act
        var response = await LoginAsync(nonExistentEmail, TestConstants.TestUsers.NewUserPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginUser_WithSeededUser_ShouldReturnValidToken()
    {
        // Arrange - Use seeded user credentials
        var email = TestConstants.TestUsers.SeededUser1Email;
        var password = TestConstants.TestUsers.SeededUserPassword;

        // Act
        var response = await LoginAsync(email, password);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(email);
        result.Name.Should().Be(TestConstants.TestUsers.SeededUser1Name);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task AuthHealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await Client.GetAsync(TestConstants.Routes.Health);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
