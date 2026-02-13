using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.UserWorkflows;

/// <summary>
/// E2E tests for transaction CRUD workflows.
/// Tests the complete transaction lifecycle: Create -> Read -> Update -> Delete.
/// </summary>
public class UserTransactionWorkflowTests : BaseE2ETest
{
    public UserTransactionWorkflowTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    [Fact]
    public async Task TransactionCrudWorkflow_CreateReadUpdateDelete_ShouldCompleteSuccessfully()
    {
        // This test uses the default test auth handler (mocked auth)
        // which authenticates as user ID 1

        // === CREATE ===
        var createRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = TestConstants.Transactions.DefaultAmount,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = TestConstants.Transactions.DefaultSubject,
            Notes = TestConstants.Transactions.DefaultNotes,
            PaymentMethod = "CREDIT_CARD",
            CategoryId = null,
            TransactionGroupId = null
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, createRequest);

        // Assert Create
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTransaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        createdTransaction.Should().NotBeNull();
        createdTransaction!.Id.Should().BeGreaterThan(0);
        createdTransaction.Subject.Should().Be(TestConstants.Transactions.DefaultSubject);
        createdTransaction.Amount.Should().Be(TestConstants.Transactions.DefaultAmount);

        var transactionId = createdTransaction.Id;

        // === READ ===
        var getResponse = await Client.GetAsync(TestConstants.Routes.Transaction(transactionId));

        // Assert Read
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedTransaction = await getResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        retrievedTransaction.Should().NotBeNull();
        retrievedTransaction!.Id.Should().Be(transactionId);
        retrievedTransaction.Subject.Should().Be(TestConstants.Transactions.DefaultSubject);

        // === UPDATE ===
        var updateRequest = new UpdateTransactionRequest
        {
            Id = transactionId,
            TransactionType = "EXPENSE",
            Amount = TestConstants.Transactions.UpdatedAmount,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Subject = TestConstants.Transactions.UpdatedSubject,
            Notes = "Updated notes",
            PaymentMethod = "DEBIT_CARD",
            CategoryId = null,
            TransactionGroupId = null
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(transactionId),
            updateRequest);

        // Assert Update
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTransaction = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updatedTransaction.Should().NotBeNull();
        updatedTransaction!.Subject.Should().Be(TestConstants.Transactions.UpdatedSubject);
        updatedTransaction.Amount.Should().Be(TestConstants.Transactions.UpdatedAmount);
        updatedTransaction.PaymentMethod.Should().Be(PaymentMethod.DEBIT_CARD);

        // Verify update persisted
        var verifyResponse = await Client.GetAsync(TestConstants.Routes.Transaction(transactionId));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifiedTransaction = await verifyResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        verifiedTransaction!.Subject.Should().Be(TestConstants.Transactions.UpdatedSubject);

        // === DELETE ===
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.Transaction(transactionId));

        // Assert Delete
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getDeletedResponse = await Client.GetAsync(TestConstants.Routes.Transaction(transactionId));
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTransaction_WithIncomeType_ShouldSucceed()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "INCOME",
            Amount = 5000.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Monthly Salary",
            Notes = "Regular income",
            PaymentMethod = "BANK_TRANSFER",
            CategoryId = null,
            TransactionGroupId = null
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.TransactionType.Should().Be(TransactionType.INCOME);
        transaction.Amount.Should().Be(5000.00m);
    }

    [Fact]
    public async Task CreateTransaction_WithAllPaymentMethods_ShouldSucceed()
    {
        var paymentMethods = new[]
        {
            ("CASH", PaymentMethod.CASH),
            ("CREDIT_CARD", PaymentMethod.CREDIT_CARD),
            ("DEBIT_CARD", PaymentMethod.DEBIT_CARD),
            ("BANK_TRANSFER", PaymentMethod.BANK_TRANSFER),
            ("PAYPAL", PaymentMethod.PAYPAL),
            ("OTHER", PaymentMethod.OTHER)
        };

        foreach (var (paymentMethodString, expectedEnum) in paymentMethods)
        {
            // Arrange
            var request = new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 10.00m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Subject = $"Payment via {paymentMethodString}",
                PaymentMethod = paymentMethodString
            };

            // Act
            var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"Expected Created for payment method {paymentMethodString}");
            var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            transaction!.PaymentMethod.Should().Be(expectedEnum);
        }
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 0m, // Invalid: must be > 0
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Invalid Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithEmptySubject_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "", // Invalid: required
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidTransactionType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "INVALID_TYPE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidPaymentMethod_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Test Transaction",
            PaymentMethod = "INVALID_METHOD"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithLowercaseEnumValues_ShouldSucceed()
    {
        // Arrange - using lowercase values (should be case-insensitive)
        var request = new CreateTransactionRequest
        {
            TransactionType = "expense",
            Amount = 25.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Lowercase Test",
            PaymentMethod = "credit_card"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.TransactionType.Should().Be(TransactionType.EXPENSE);
        transaction.PaymentMethod.Should().Be(PaymentMethod.CREDIT_CARD);
    }

    [Fact]
    public async Task GetTransaction_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = 999999;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.Transaction(nonExistentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTransaction_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidId = -1;

        // Act
        var response = await Client.GetAsync(TestConstants.Routes.Transaction(invalidId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTransaction_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = 999999;
        var request = new UpdateTransactionRequest
        {
            Id = nonExistentId,
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Update Non-existent",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(nonExistentId),
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTransaction_WithInvalidTransactionType_ShouldReturnBadRequest()
    {
        // Arrange - First create a valid transaction
        var createRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "To Be Updated",
            PaymentMethod = "CASH"
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act - Update with invalid transaction type
        var updateRequest = new UpdateTransactionRequest
        {
            Id = created!.Id,
            TransactionType = "INVALID_TYPE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Updated",
            PaymentMethod = "CASH"
        };

        var response = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(created.Id),
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteTransaction_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = 999999;

        // Act
        var response = await Client.DeleteAsync(TestConstants.Routes.Transaction(nonExistentId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMultipleTransactions_ThenReadAll_ShouldReturnCorrectData()
    {
        // Arrange - Create multiple transactions
        var subjects = new[] { "Transaction A", "Transaction B", "Transaction C" };
        var createdIds = new List<int>();

        foreach (var subject in subjects)
        {
            var request = new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 25.00m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Subject = subject,
                PaymentMethod = "CASH"
            };

            var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            createdIds.Add(transaction!.Id);
        }

        // Act & Assert - Verify each can be retrieved
        foreach (var (id, index) in createdIds.Select((id, i) => (id, i)))
        {
            var response = await Client.GetAsync(TestConstants.Routes.Transaction(id));
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            transaction!.Subject.Should().Be(subjects[index]);
        }
    }

    [Fact]
    public async Task CreateTransaction_WithFutureDate_ShouldSucceed()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = futureDate,
            Subject = "Future Scheduled Payment",
            PaymentMethod = "BANK_TRANSFER"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.Date.Should().Be(futureDate);
    }

    [Fact]
    public async Task CreateTransaction_WithPastDate_ShouldSucceed()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-365));
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = pastDate,
            Subject = "Historical Transaction",
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.Date.Should().Be(pastDate);
    }

    [Fact]
    public async Task CreateTransaction_WithMaxLengthNotes_ShouldSucceed()
    {
        // Arrange
        var longNotes = new string('A', 2000); // Max length is 2000
        var request = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 50.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Transaction with long notes",
            Notes = longNotes,
            PaymentMethod = "CASH"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.Notes.Should().Be(longNotes);
    }

    [Fact]
    public async Task UpdateTransaction_ChangingType_FromExpenseToIncome_ShouldSucceed()
    {
        // Arrange - Create expense
        var createRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Originally Expense",
            PaymentMethod = "CASH"
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act - Update to income
        var updateRequest = new UpdateTransactionRequest
        {
            Id = created!.Id,
            TransactionType = "INCOME", // Changed from EXPENSE to INCOME
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Now Income",
            PaymentMethod = "BANK_TRANSFER"
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(created.Id),
            updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updated!.TransactionType.Should().Be(TransactionType.INCOME);
        updated.Subject.Should().Be("Now Income");
    }

    [Fact]
    public async Task CreateTransaction_WithMixedCaseEnumValues_ShouldSucceed()
    {
        // Arrange - using mixed case values
        var request = new CreateTransactionRequest
        {
            TransactionType = "Expense",
            Amount = 25.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Mixed Case Test",
            PaymentMethod = "Credit_Card"
        };

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction!.TransactionType.Should().Be(TransactionType.EXPENSE);
        transaction.PaymentMethod.Should().Be(PaymentMethod.CREDIT_CARD);
    }
}
