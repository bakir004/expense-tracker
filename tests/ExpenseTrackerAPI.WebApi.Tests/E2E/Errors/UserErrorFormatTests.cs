using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Errors;

/// <summary>
/// Tests RFC 9110 compliant error response formats for User endpoints.
/// Validates that all error responses follow proper structure and error keys don't contain dots.
/// </summary>
public class UserErrorFormatTests : BaseE2ETest
{
    public UserErrorFormatTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Update Profile Error Format Tests

    [Fact]
    public async Task UpdateProfile_WithInvalidEmailOnly_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Since Name is now optional, test with invalid email instead
        var request = new UpdateUserRequest(
            Name: null,
            Email: "invalid-email",
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: null);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

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
        problemDetails.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidEmail_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: "invalid-email-format",
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

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
    public async Task UpdateProfile_WithWrongCurrentPassword_ShouldReturnUnauthorizedProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: TestConstants.TestUsers.SeededUser1Email,
            NewPassword: null,
            CurrentPassword: "WrongPassword123!",
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(401);
        problemDetails.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateProfile_WithWeakNewPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: TestConstants.TestUsers.SeededUser1Email,
            NewPassword: "weakpassword",
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("NewPassword");
    }

    [Fact]
    public async Task UpdateProfile_WithShortNewPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: TestConstants.TestUsers.SeededUser1Email,
            NewPassword: "Short1!",
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("NewPassword");
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyCurrentPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: TestConstants.TestUsers.SeededUser1Email,
            NewPassword: null,
            CurrentPassword: "",
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("CurrentPassword");
    }

    [Fact]
    public async Task UpdateProfile_WithExcessivelyLongName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var tooLongName = new string('A', 101); // Exceeds 100 char limit
        var request = new UpdateUserRequest(
            Name: tooLongName,
            Email: TestConstants.TestUsers.SeededUser1Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task UpdateProfile_WithMultipleValidationErrors_ShouldReturnAllErrorsWithoutDotsInKeys()
    {
        // Arrange - Multiple validation errors
        var request = new UpdateUserRequest(
            Name: "",
            Email: "invalid-email",
            NewPassword: "weak",
            CurrentPassword: "",
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

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
    public async Task UpdateProfile_WithDuplicateEmail_ShouldReturnConflictProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Try to change to another user's email
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: TestConstants.TestUsers.SeededUser2Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(409);
        problemDetails.Title.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Delete Profile Error Format Tests

    [Fact]
    public async Task DeleteProfile_WithoutConfirmation_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new DeleteUserRequest(
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            ConfirmDeletion: false);

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(TestConstants.Routes.UserProfile, UriKind.Relative),
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Confirmation");
    }

    [Fact]
    public async Task DeleteProfile_WithWrongPassword_ShouldReturnUnauthorizedProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new DeleteUserRequest(
            CurrentPassword: "WrongPassword123!",
            ConfirmDeletion: true);

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(TestConstants.Routes.UserProfile, UriKind.Relative),
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(401);
    }

    [Fact]
    public async Task DeleteProfile_WithEmptyPassword_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new DeleteUserRequest(
            CurrentPassword: "",
            ConfirmDeletion: true);

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri(TestConstants.Routes.UserProfile, UriKind.Relative),
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("CurrentPassword");
    }

    #endregion

    #region RFC 9110 Compliance Tests

    [Fact]
    public async Task AllUserErrors_ShouldHaveProperStatusCodes()
    {
        // Test that error responses have status codes matching HTTP semantics

        // 400 Bad Request - validation error
        var validationRequest = new UpdateUserRequest("", "invalid", null, "", 0m);
        var validationResponse = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, validationRequest);
        validationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 401 Unauthorized - wrong password
        var authRequest = new UpdateUserRequest(
            "Name",
            TestConstants.TestUsers.SeededUser1Email,
            null,
            "WrongPassword!",
            0m);
        var authResponse = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, authRequest);
        authResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 409 Conflict - duplicate email
        var conflictRequest = new UpdateUserRequest(
            "Name",
            TestConstants.TestUsers.SeededUser2Email,
            null,
            TestConstants.TestUsers.SeededUserPassword,
            0m);
        var conflictResponse = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, conflictRequest);
        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AllUserValidationErrors_ShouldIncludeRequiredProblemDetailsFields()
    {
        // Arrange
        var request = new UpdateUserRequest("", "invalid", null, "", 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

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
    public async Task AllUserErrors_ShouldHaveTraceIdForDebugging()
    {
        // Arrange
        var request = new UpdateUserRequest("", "invalid", null, "", 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        // TraceId helps with debugging and correlation
        problemDetails!.TraceId.Should().NotBeNullOrEmpty("traceId should be present for request tracking");
    }

    [Theory]
    [InlineData("", "valid@email.com", null, "Password123!")]
    [InlineData("Valid Name", "invalid-email", null, "Password123!")]
    [InlineData("Valid Name", "valid@email.com", "weak", "Password123!")]
    [InlineData("Valid Name", "valid@email.com", null, "")]
    public async Task UpdateProfile_ValidationErrors_ShouldNeverContainDotsInErrorKeys(
        string name, string email, string? newPassword, string currentPassword)
    {
        // Arrange
        var request = new UpdateUserRequest(name, email, newPassword, currentPassword, 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

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

    [Fact]
    public async Task UpdateProfile_WithNegativeInitialBalance_ShouldHandleAppropriately()
    {
        // Arrange
        var request = new UpdateUserRequest(
            Name: "Valid Name",
            Email: TestConstants.TestUsers.SeededUser1Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: -100.00m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, request);

        // Assert - May be OK or BadRequest depending on business rules
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails != null)
            {
                ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue();
            }
        }
    }

    #endregion
}
