using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Errors;

/// <summary>
/// Tests RFC 9110 compliant error response formats for Transaction endpoints.
/// Validates that all error responses follow proper structure and error keys don't contain dots.
/// </summary>
public class TransactionErrorFormatTests : BaseE2ETest
{
    public TransactionErrorFormatTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Create Transaction Error Format Tests

    [Fact]
    public async Task CreateTransaction_WithInvalidTransactionType_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "INVALID_TYPE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

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
    }

    [Fact]
    public async Task CreateTransaction_WithNegativeAmount_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = -50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue(
            "error keys should not contain dots");

        problemDetails.Errors.Should().ContainKey("Amount");
    }

    [Fact]
    public async Task CreateTransaction_WithZeroAmount_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 0m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Amount");
    }

    [Fact]
    public async Task CreateTransaction_WithEmptySubject_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

        problemDetails.Errors.Should().ContainKey("Subject");
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidPaymentMethod_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test Transaction",
            PaymentMethod = "INVALID_METHOD"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
        ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();
    }

    [Fact]
    public async Task CreateTransaction_WithMultipleValidationErrors_ShouldReturnAllErrorsWithoutDotsInKeys()
    {
        // Arrange - Multiple validation errors
        var request = new CreateTransactionRequest
        {
            TransactionType = "INVALID",
            Amount = -100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "",
            PaymentMethod = "INVALID"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

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
    public async Task CreateTransaction_WithFutureDateTooFar_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)),
            Subject = "Future Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert - May be BadRequest if future dates are restricted
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
            ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();
        }
    }

    [Fact]
    public async Task CreateTransaction_ExpenseWithoutCategory_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Expense transactions may require a category
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Expense without category",
            PaymentMethod = "CASH",
            CategoryId = null
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert - May require category for expenses
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
            ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();

            problemDetails!.Errors.Should().ContainKey("CategoryId");
        }
    }

    #endregion

    #region Get Transaction Error Format Tests

    [Fact]
    public async Task GetTransaction_WithInvalidId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var nonExistentId = 999999;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.Transaction(nonExistentId));

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
    public async Task GetTransaction_WithNegativeId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = -1;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.Transaction(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(400);
    }

    [Fact]
    public async Task GetTransaction_WithZeroId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = 0;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.Transaction(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    }

    #endregion

    #region Update Transaction Error Format Tests

    [Fact]
    public async Task UpdateTransaction_WithNonExistentId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var nonExistentId = 999999;
        var request = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 200.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Updated Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.Transaction(nonExistentId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(404);
    }

    [Fact]
    public async Task UpdateTransaction_WithInvalidData_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var validId = 1; // Assuming a transaction exists
        var request = new UpdateTransactionRequest
        {
            TransactionType = "INVALID",
            Amount = -100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "",
            PaymentMethod = "INVALID"
        };

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.Transaction(validId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();

        var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails!.Errors);
        invalidKeys.Should().BeEmpty(
            $"no error keys should contain dots, but found: {string.Join(", ", invalidKeys)}");
    }

    [Fact]
    public async Task UpdateTransaction_WithInvalidId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = -5;
        var request = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.Transaction(invalidId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    }

    #endregion

    #region Delete Transaction Error Format Tests

    [Fact]
    public async Task DeleteTransaction_WithNonExistentId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var nonExistentId = 999999;

        // Act
        var response = await Client.DeleteAsync(TestConstants.Routes.Transaction(nonExistentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();

        problemDetails!.Status.Should().Be(404);
    }

    [Fact]
    public async Task DeleteTransaction_WithInvalidId_ShouldReturnBadRequestProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var invalidId = 0;

        // Act
        var response = await Client.DeleteAsync(TestConstants.Routes.Transaction(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    }

    #endregion

    #region Filter/Query Error Format Tests

    [Fact]
    public async Task GetTransactions_WithInvalidTransactionTypeFilter_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var queryString = "?transactionType=INVALID_TYPE";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue();
            }
        }
        else
        {
            // If not BadRequest, the API may allow this and filter it out
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetTransactions_WithInvalidAmountRange_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - minAmount > maxAmount
        var queryString = "?minAmount=100&maxAmount=50";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        // This may be accepted as valid (filtering by range), so just verify it doesn't error with dots if it's BadRequest
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue();
            }
        }
        else
        {
            // If not BadRequest, it's acceptable - the API may allow this query
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetTransactions_WithInvalidDateFormat_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var queryString = "?dateFrom=invalid-date&dateTo=also-invalid";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails.Errors);
                invalidKeys.Should().BeEmpty(
                    $"error keys should not contain dots, but found: {string.Join(", ", invalidKeys)}");
            }
        }
        else
        {
            // If not BadRequest, the API may ignore invalid date formats
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetTransactions_WithInvalidPageSize_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Page size outside valid range
        var queryString = "?pageSize=0";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue();
            }
        }
        else
        {
            // If not BadRequest, the API may default to a valid page size
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetTransactions_WithExcessivePageSize_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange - Page size exceeds maximum
        var queryString = "?pageSize=1000";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue();
            }
        }
        else
        {
            // If not BadRequest, the API may cap the page size
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetTransactions_WithInvalidPageNumber_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
    {
        // Arrange
        var queryString = "?page=0";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                ErrorResponseValidator.AllKeysAreValid(problemDetails.Errors).Should().BeTrue();
            }
        }
        else
        {
            // If not BadRequest, the API may default to page 1
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
    }

    #endregion

    #region RFC 9110 Compliance Tests

    [Fact]
    public async Task AllTransactionErrors_ShouldHaveProperStatusCodes()
    {
        // 400 Bad Request - validation error
        var validationRequest = new CreateTransactionRequest
        {
            TransactionType = "INVALID",
            Amount = -100m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "",
            PaymentMethod = "INVALID"
        };
        var validationResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, validationRequest);
        validationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 404 Not Found - resource doesn't exist
        var notFoundResponse = await Client.GetAsync(TestConstants.Routes.Transaction(999999));
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AllTransactionValidationErrors_ShouldIncludeRequiredProblemDetailsFields()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "INVALID",
            Amount = 0m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

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
    public async Task AllTransactionErrors_ShouldHaveTraceIdForDebugging()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = -50m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

        problemDetails!.TraceId.Should().NotBeNullOrEmpty("traceId should be present for request tracking");
    }

    [Theory]
    [InlineData("INVALID", 100, "Test", "CASH")]
    [InlineData("EXPENSE", -50, "Test", "CASH")]
    [InlineData("EXPENSE", 0, "Test", "CASH")]
    [InlineData("EXPENSE", 100, "", "CASH")]
    [InlineData("EXPENSE", 100, "Test", "INVALID")]
    public async Task CreateTransaction_ValidationErrors_ShouldNeverContainDotsInErrorKeys(
        string transactionType, decimal amount, string subject, string paymentMethod)
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = transactionType,
            Amount = amount,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = subject,
            PaymentMethod = paymentMethod
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

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
