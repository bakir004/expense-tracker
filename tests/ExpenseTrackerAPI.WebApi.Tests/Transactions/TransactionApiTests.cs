using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.WebApi.Tests.Transactions;

public class TransactionApiTests : BaseApiTest
{
    public TransactionApiTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateTransaction_ShouldSaveToRealDb()
    {
        // 1. Arrange: Define a valid payload
        var payload = new CreateTransactionRequest
        {
            TransactionType = TransactionType.EXPENSE,
            Amount = 100.50m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Utility Bill",
            Notes = "Monthly electricity",
            PaymentMethod = PaymentMethod.BANK_TRANSFER,
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
        savedTransaction.TransactionType.Should().Be(TransactionType.EXPENSE);
        savedTransaction.Subject.Should().Be("Utility Bill");
    }
}
