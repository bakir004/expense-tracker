using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Transactions;

/// <summary>
/// E2E tests for transaction API endpoints.
/// Tests transaction creation, validation, and database persistence.
/// </summary>
public class TransactionApiTests : BaseE2ETest
{
    public TransactionApiTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateTransaction_ShouldSaveToRealDb()
    {
        // 1. Arrange: Define a valid payload with string values for enums
        var payload = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.50m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Utility Bill",
            Notes = "Monthly electricity",
            PaymentMethod = "BANK_TRANSFER",
            CategoryId = null,
            TransactionGroupId = null
        };

        // 2. Act: Hit the actual endpoint
        var response = await Client.PostAsJsonAsync("/api/v1/transactions", payload);

        // 3. Assert: Response is correct
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Verify E2E: Go behind the API's back and check the REAL Postgres DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedTransaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.Subject == "Utility Bill");

        savedTransaction.Should().NotBeNull();
        savedTransaction!.Amount.Should().Be(100.50m);
        savedTransaction.TransactionType.Should().Be(TransactionType.EXPENSE); // Domain entity still uses enum
        savedTransaction.Subject.Should().Be("Utility Bill");
    }

    [Fact]
    public async Task CreateTransaction_WithLowercaseTransactionType_ShouldSucceed()
    {
        // Arrange: Use lowercase for transaction type (should be case-insensitive)
        var payload = new CreateTransactionRequest
        {
            TransactionType = "expense",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Lowercase Type Test",
            PaymentMethod = "cash"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/transactions", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.TransactionType.Should().Be("EXPENSE");
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidTransactionType_ShouldReturnBadRequest()
    {
        // Arrange: Use invalid transaction type
        var payload = new CreateTransactionRequest
        {
            TransactionType = "INVALID_TYPE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Invalid Type Test",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/transactions", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidPaymentMethod_ShouldReturnBadRequest()
    {
        // Arrange: Use invalid payment method
        var payload = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Invalid Payment Test",
            PaymentMethod = "INVALID_PAYMENT"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/transactions", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithEmptyTransactionType_ShouldReturnBadRequest()
    {
        // Arrange: Use empty transaction type
        var payload = new CreateTransactionRequest
        {
            TransactionType = "",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Empty Type Test",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/transactions", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithAllPaymentMethodStrings_ShouldSucceed()
    {
        var paymentMethods = new[]
        {
            "CASH",
            "CREDIT_CARD",
            "DEBIT_CARD",
            "BANK_TRANSFER",
            "MOBILE_PAYMENT",
            "PAYPAL",
            "CRYPTO",
            "OTHER"
        };

        foreach (var paymentMethod in paymentMethods)
        {
            // Arrange
            var request = new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 10.00m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Subject = $"Payment via {paymentMethod}",
                PaymentMethod = paymentMethod
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/transactions", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"Expected Created for payment method {paymentMethod}");
        }
    }
}
