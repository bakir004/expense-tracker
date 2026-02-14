using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Errors;

/// <summary>
/// Tests RFC 9110 compliant error response formats for Authentication endpoints.
/// Validates that all error responses follow proper structure and error keys don't contain dots.
/// </summary>
public class AuthErrorFormatTests : BaseE2ETest
{
    public AuthErrorFormatTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Register Endpoint Error Format Tests

    [Fact]
    public async Task Register_WithEmptyName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "",
            Email: GenerateUniqueEmail(),
            Password: TestConstants.TestUsers.NewUserPassword,
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue(
            "response should conform to RFC 9110 ProblemDetails format");

        // Validate no dots in error keys
        var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails!.Errors);
        invalidKeys.Should().BeEmpty(
            $"error keys should not contain dots, but found: {string.Join(", ", invalidKeys)}");

        problemDetails.Status.Should().Be(400);
        problemDetails.Errors.Should().NotBeNull();
        problemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "Test User",
            Email: "invalid-email-format",
            Password: TestConstants.TestUsers.NewUserPassword,
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();

        // Validate no dots in error keys
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue(
            "error keys should not contain dots");

        problemDetails.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Password without special character
        var request = new RegisterRequest(
            Name: "Test User",
            Email: GenerateUniqueEmail(),
            Password: "WeakPassword123",
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();

        // Validate no dots in error keys
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Password");
    }

    [Fact]
    public async Task Register_WithShortPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "Test User",
            Email: GenerateUniqueEmail(),
            Password: "Short1!",
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Password");
    }

    [Fact]
    public async Task Register_WithMultipleValidationErrors_ShouldReturnAllErrorsWithoutDotsInKeys()
    {
        // Arrange - Multiple validation errors
        var request = new RegisterRequest(
            Name: "",
            Email: "invalid-email",
            Password: "weak",
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();

        // Validate no dots in ANY error keys
        var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails!.Errors);
        invalidKeys.Should().BeEmpty(
            $"no error keys should contain dots, but found: {string.Join(", ", invalidKeys)}");

        // Should have multiple errors
        problemDetails.Errors.Should().NotBeNull();
        problemDetails.Errors!.Count.Should().BeGreaterThan(1, "multiple validation errors should be present");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflictProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Use existing seeded user email
        var request = new RegisterRequest(
            Name: "Test User",
            Email: TestConstants.TestUsers.SeededUser1Email,
            Password: TestConstants.TestUsers.NewUserPassword,
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(409);
        problemDetails.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithExcessivelyLongName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var tooLongName = new string('A', 101); // Exceeds 100 char limit
        var request = new RegisterRequest(
            Name: tooLongName,
            Email: GenerateUniqueEmail(),
            Password: TestConstants.TestUsers.NewUserPassword,
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Register_WithMissingPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new RegisterRequest(
            Name: "Test User",
            Email: GenerateUniqueEmail(),
            Password: "",
            InitialBalance: null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Password");
    }

    #endregion

    #region Login Endpoint Error Format Tests

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "",
            Password: TestConstants.TestUsers.SeededUserPassword);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new LoginRequest(
            Email: TestConstants.TestUsers.SeededUser1Email,
            Password: "");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Password");
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "not-an-email",
            Password: TestConstants.TestUsers.SeededUserPassword);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ShouldReturnUnauthorizedProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new LoginRequest(
            Email: TestConstants.TestUsers.SeededUser1Email,
            Password: "WrongPassword123!");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(401);
        problemDetails.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorizedProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "nonexistent.user@test.com",
            Password: TestConstants.TestUsers.NewUserPassword);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithMultipleValidationErrors_ShouldReturnAllErrorsWithoutDotsInKeys()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "",
            Password: "");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();

        var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails!.Errors);
        invalidKeys.Should().BeEmpty(
            $"no error keys should contain dots, but found: {string.Join(", ", invalidKeys)}");

        problemDetails.Errors.Should().NotBeNull();
        problemDetails.Errors!.Count.Should().BeGreaterThan(1);
    }

    #endregion

    #region RFC 9110 Compliance Tests

    [Fact]
    public async Task AllAuthErrors_ShouldHaveProperStatusCodes()
    {
        // Test that error responses have status codes matching HTTP semantics

        // 400 Bad Request - validation error
        var validationRequest = new RegisterRequest("", "invalid", "weak", null);
        var validationResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, validationRequest);
        validationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 401 Unauthorized - authentication failure
        var authRequest = new LoginRequest("user@test.com", "WrongPassword!");
        var authResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, authRequest);
        authResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 409 Conflict - duplicate resource
        var conflictRequest = new RegisterRequest(
            "User",
            TestConstants.TestUsers.SeededUser1Email,
            TestConstants.TestUsers.NewUserPassword,
            null);
        var conflictResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, conflictRequest);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AllAuthValidationErrors_ShouldIncludeRequiredProblemDetailsFields()
    {
        // Arrange
        var request = new RegisterRequest("", "invalid", "weak", null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        // RFC 9110 required fields
        problemDetails!.Status.Should().NotBeNull("status is required by RFC 9110");
        problemDetails.Status.Should().Be((int)response.StatusCode, "status should match response code");

        // At least one of title or detail should be present
        var hasDescription = !string.IsNullOrWhiteSpace(problemDetails.Title) ||
                            !string.IsNullOrWhiteSpace(problemDetails.Detail);
        hasDescription.Should().BeTrue("either title or detail is required for problem description");
    }

    [Fact]
    public async Task AllAuthErrors_ShouldHaveTraceIdForDebugging()
    {
        // Arrange
        var request = new RegisterRequest("", "invalid", "weak", null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        // TraceId helps with debugging and correlation
        problemDetails!.TraceId.Should().NotBeNullOrEmpty("traceId should be present for request tracking");
    }

    [Theory]
    [InlineData("", "valid@email.com", "Password123!")]
    [InlineData("Valid Name", "", "Password123!")]
    [InlineData("Valid Name", "invalid-email", "Password123!")]
    [InlineData("Valid Name", "valid@email.com", "")]
    [InlineData("Valid Name", "valid@email.com", "short")]
    public async Task Register_ValidationErrors_ShouldNeverContainDotsInErrorKeys(
        string name, string email, string password)
    {
        // Arrange
        var request = new RegisterRequest(name, email, password, null);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails.Errors);
                invalidKeys.Should().BeEmpty(
                    $"error keys must not contain dots. Found invalid keys: {string.Join(", ", invalidKeys)}");
            }
        }
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("invalid-email", "Password123!")]
    [InlineData("valid@email.com", "")]
    public async Task Login_ValidationErrors_ShouldNeverContainDotsInErrorKeys(
        string email, string password)
    {
        // Arrange
        var request = new LoginRequest(email, password);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue(
                    "all error keys must be valid (no dots)");
            }
        }
    }

    #endregion
}
