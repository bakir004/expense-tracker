using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;
using Xunit.Abstractions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Errors;

/// <summary>
/// Diagnostic test to inspect actual error response formats and keys.
/// </summary>
public class DiagnosticErrorTest : BaseE2ETest
{
    private readonly ITestOutputHelper _output;

    public DiagnosticErrorTest(ExpenseTrackerApiFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task Diagnostic_ShowErrorKeysForInvalidPageSize()
    {
        // Arrange
        var queryString = "?pageSize=0";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        _output.WriteLine($"Status Code: {response.StatusCode}");
        _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {content}");

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                _output.WriteLine("\nError Keys:");
                foreach (var key in problemDetails.Errors.Keys)
                {
                    _output.WriteLine($"  - '{key}' (Contains dot: {key.Contains('.')})");
                    foreach (var message in problemDetails.Errors[key])
                    {
                        _output.WriteLine($"      Message: {message}");
                    }
                }

                var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails.Errors);
                if (invalidKeys.Any())
                {
                    _output.WriteLine("\nINVALID KEYS (contain dots):");
                    foreach (var key in invalidKeys)
                    {
                        _output.WriteLine($"  - '{key}'");
                    }
                }
            }
        }

        // This test always passes - it's just for diagnostics
        Assert.True(true);
    }

    [Fact]
    public async Task Diagnostic_ShowErrorKeysForInvalidTransactionType()
    {
        // Arrange
        var queryString = "?transactionType=INVALID_TYPE";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        _output.WriteLine($"Status Code: {response.StatusCode}");
        _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {content}");

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                _output.WriteLine("\nError Keys:");
                foreach (var key in problemDetails.Errors.Keys)
                {
                    _output.WriteLine($"  - '{key}' (Contains dot: {key.Contains('.')})");
                    foreach (var message in problemDetails.Errors[key])
                    {
                        _output.WriteLine($"      Message: {message}");
                    }
                }

                var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails.Errors);
                if (invalidKeys.Any())
                {
                    _output.WriteLine("\nINVALID KEYS (contain dots):");
                    foreach (var key in invalidKeys)
                    {
                        _output.WriteLine($"  - '{key}'");
                    }
                }
            }
        }

        // This test always passes - it's just for diagnostics
        Assert.True(true);
    }

    [Fact]
    public async Task Diagnostic_ShowErrorKeysForInvalidDateFormat()
    {
        // Arrange
        var queryString = "?dateFrom=invalid-date";

        // Act
        var response = await Client.GetAsync($"{TestConstants.Routes.Transactions}{queryString}");

        // Assert
        _output.WriteLine($"Status Code: {response.StatusCode}");
        _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {content}");

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();

            if (problemDetails?.Errors != null)
            {
                _output.WriteLine("\nError Keys:");
                foreach (var key in problemDetails.Errors.Keys)
                {
                    _output.WriteLine($"  - '{key}' (Contains dot: {key.Contains('.')})");
                    foreach (var message in problemDetails.Errors[key])
                    {
                        _output.WriteLine($"      Message: {message}");
                    }
                }

                var invalidKeys = ErrorResponseValidator.GetInvalidKeys(problemDetails.Errors);
                if (invalidKeys.Any())
                {
                    _output.WriteLine("\nINVALID KEYS (contain dots):");
                    foreach (var key in invalidKeys)
                    {
                        _output.WriteLine($"  - '{key}'");
                    }
                }
            }
        }

        // This test always passes - it's just for diagnostics
        Assert.True(true);
    }
}
