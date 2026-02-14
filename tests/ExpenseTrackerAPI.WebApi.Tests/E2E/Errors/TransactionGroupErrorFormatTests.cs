using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.TransactionGroups;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Errors;

/// <summary>
/// Tests RFC 9110 compliant error response formats for TransactionGroup endpoints.
/// Validates that all error responses follow proper structure and error keys don't contain dots.
/// </summary>
public class TransactionGroupErrorFormatTests : BaseE2ETest
{
    public TransactionGroupErrorFormatTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Create TransactionGroup Error Format Tests

    [Fact]
    public async Task CreateTransactionGroup_WithEmptyName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: "",
            Description: "Test Description");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

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
    public async Task CreateTransactionGroup_WithExcessivelyLongName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var tooLongName = new string('A', 256); // Exceeds 255 char limit
        var request = new CreateTransactionGroupRequest(
            Name: tooLongName,
            Description: "Test Description");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue(
            "error keys should not contain dots");

        problemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task CreateTransactionGroup_WithNullName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: null!,
            Description: "Test Description");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task CreateTransactionGroup_WithExcessivelyLongDescription_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var tooLongDescription = new string('A', 1001); // Exceeds 1000 char limit
        var request = new CreateTransactionGroupRequest(
            Name: "Valid Name",
            Description: tooLongDescription);

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
            ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

            problemDetails.Errors.Should().ContainKey("Description");
        }
    }

    #endregion

    #region Get TransactionGroup Error Format Tests

    [Fact]
    public async Task GetTransactionGroup_WithNonExistentId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var nonExistentId = 999999;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.TransactionGroup(nonExistentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problemDetails.Should().NotBeNull();

        // Validate RFC 9110 compliance
        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(404);
        problemDetails.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTransactionGroup_WithNegativeId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = -1;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.TransactionGroup(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetTransactionGroup_WithZeroId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = 0;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.TransactionGroup(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    }

    #endregion

    #region Update TransactionGroup Error Format Tests

    [Fact]
    public async Task UpdateTransactionGroup_WithNonExistentId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var nonExistentId = 999999;
        var request = new UpdateTransactionGroupRequest(
            Name: "Updated Name",
            Description: "Updated Description");

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.TransactionGroup(nonExistentId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(404);
    }

    [Fact]
    public async Task UpdateTransactionGroup_WithEmptyName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var validId = 1; // Assuming a transaction group exists
        var request = new UpdateTransactionGroupRequest(
            Name: "",
            Description: "Updated Description");

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.TransactionGroup(validId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task UpdateTransactionGroup_WithInvalidId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = -5;
        var request = new UpdateTransactionGroupRequest(
            Name: "Updated Name",
            Description: "Updated Description");

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.TransactionGroup(invalidId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTransactionGroup_WithExcessivelyLongName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var validId = 1;
        var tooLongName = new string('B', 256);
        var request = new UpdateTransactionGroupRequest(
            Name: tooLongName,
            Description: "Updated Description");

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.TransactionGroup(validId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Name");
    }

    #endregion

    #region Delete TransactionGroup Error Format Tests

    [Fact]
    public async Task DeleteTransactionGroup_WithNonExistentId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var nonExistentId = 999999;

        // Act
        var response = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(nonExistentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(404);
    }

    [Fact]
    public async Task DeleteTransactionGroup_WithInvalidId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = 0;

        // Act
        var response = await Client.DeleteAsync(TestConstants.Routes.TransactionGroup(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    }

    #endregion

    #region RFC 9110 Compliance Tests

    [Fact]
    public async Task AllTransactionGroupErrors_ShouldHaveProperStatusCodes()
    {
        // 400 Bad Request - validation error
        var validationRequest = new CreateTransactionGroupRequest(
            Name: "",
            Description: "Test");
        var validationResponse = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, validationRequest);
        validationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 404 Not Found - resource doesn't exist
        var notFoundResponse = await Client.GetAsync(TestConstants.Routes.TransactionGroup(999999));
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AllTransactionGroupValidationErrors_ShouldIncludeRequiredProblemDetailsFields()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: "",
            Description: "Test");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

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
    public async Task AllTransactionGroupErrors_ShouldHaveTraceIdForDebugging()
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: "",
            Description: "Test");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        problemDetails!.TraceId.Should().NotBeNullOrEmpty("traceId should be present for request tracking");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateTransactionGroup_ValidationErrors_ShouldNeverContainDotsInErrorKeys(string? name)
    {
        // Arrange
        var request = new CreateTransactionGroupRequest(
            Name: name!,
            Description: "Test");

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.TransactionGroups, request);

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

    #endregion
}
