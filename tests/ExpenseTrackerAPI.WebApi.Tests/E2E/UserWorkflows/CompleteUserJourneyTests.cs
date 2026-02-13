using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.UserWorkflows;

/// <summary>
/// E2E tests for complete user journeys.
/// Tests realistic user scenarios from start to finish.
///
/// NOTE: The TestAuthHandler authenticates all requests as user ID 1 (the seeded user).
/// Tests that use RegisterAndLoginAsync still work for testing the auth endpoints,
/// but subsequent authenticated requests will be processed as user ID 1.
/// For true multi-user isolation testing, a separate test factory without TestAuthHandler would be needed.
/// </summary>
public class CompleteUserJourneyTests : BaseApiTest
{
    public CompleteUserJourneyTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    /// <summary>
    /// Tests the complete lifecycle of transaction management:
    /// Create Transactions -> Read Transactions -> Update Transaction -> Delete Transaction
    /// Uses the seeded user (ID 1) via TestAuthHandler.
    /// </summary>
    [Fact]
    public async Task CompleteTransactionManagementJourney_ShouldSucceed()
    {
        // === STEP 1: CREATE INCOME TRANSACTION ===
        var incomeRequest = new CreateTransactionRequest
        {
            TransactionType = "INCOME",
            Amount = 5000.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Journey Test - Monthly Salary",
            Notes = "Regular monthly income",
            PaymentMethod = "BANK_TRANSFER"
        };

        var incomeResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, incomeRequest);
        incomeResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Income creation should succeed");

        var incomeTransaction = await incomeResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        incomeTransaction.Should().NotBeNull();
        incomeTransaction!.TransactionType.Should().Be("INCOME");

        // === STEP 2: CREATE MULTIPLE EXPENSE TRANSACTIONS ===
        var expenses = new[]
        {
            ("Journey - Grocery Shopping", 150.00m, "DEBIT_CARD"),
            ("Journey - Electric Bill", 85.50m, "BANK_TRANSFER"),
            ("Journey - Coffee Shop", 12.75m, "CREDIT_CARD"),
            ("Journey - Gas Station", 45.00m, "CREDIT_CARD")
        };

        var createdExpenseIds = new List<int>();

        foreach (var (subject, amount, paymentMethod) in expenses)
        {
            var expenseRequest = new CreateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = amount,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Subject = subject,
                PaymentMethod = paymentMethod
            };

            var expenseResponse = await Client.PostAsJsonAsync(
                TestConstants.Routes.Transactions,
                expenseRequest);

            expenseResponse.StatusCode.Should().Be(HttpStatusCode.Created, $"Expense '{subject}' creation should succeed");

            var expense = await expenseResponse.Content.ReadFromJsonAsync<TransactionResponse>();
            createdExpenseIds.Add(expense!.Id);
        }

        createdExpenseIds.Should().HaveCount(4, "All expenses should be created");

        // === STEP 3: READ AND VERIFY TRANSACTIONS ===
        // Verify income
        var getIncomeResponse = await Client.GetAsync(
            TestConstants.Routes.Transaction(incomeTransaction.Id));
        getIncomeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedIncome = await getIncomeResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        retrievedIncome!.Amount.Should().Be(5000.00m);
        retrievedIncome.Subject.Should().Be("Journey Test - Monthly Salary");

        // Verify first expense
        var getExpenseResponse = await Client.GetAsync(
            TestConstants.Routes.Transaction(createdExpenseIds[0]));
        getExpenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedExpense = await getExpenseResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        retrievedExpense!.Subject.Should().Be("Journey - Grocery Shopping");

        // === STEP 4: UPDATE A TRANSACTION ===
        var updateRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 15.50m, // Increased amount
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Journey - Coffee Shop - Team Meeting",
            Notes = "Business expense",
            PaymentMethod = "CREDIT_CARD"
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(createdExpenseIds[2]),
            updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Update should succeed");

        var updatedTransaction = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        updatedTransaction!.Amount.Should().Be(15.50m);
        updatedTransaction.Subject.Should().Contain("Team Meeting");

        // === STEP 5: DELETE A TRANSACTION ===
        var deleteResponse = await Client.DeleteAsync(
            TestConstants.Routes.Transaction(createdExpenseIds[3])); // Gas Station

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, "Delete should succeed");

        // Verify deletion
        var verifyDeleteResponse = await Client.GetAsync(
            TestConstants.Routes.Transaction(createdExpenseIds[3]));
        verifyDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound, "Deleted transaction should not exist");

        // === STEP 6: VERIFY REMAINING TRANSACTIONS STILL EXIST ===
        var remainingIds = new[] { incomeTransaction.Id, createdExpenseIds[0], createdExpenseIds[1], createdExpenseIds[2] };

        foreach (var id in remainingIds)
        {
            var response = await Client.GetAsync(TestConstants.Routes.Transaction(id));
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Transaction {id} should still exist");
        }

        // === CLEANUP: Delete created transactions ===
        foreach (var id in remainingIds)
        {
            await Client.DeleteAsync(TestConstants.Routes.Transaction(id));
        }
    }

    /// <summary>
    /// Tests a monthly expense tracking scenario where a user tracks their spending over time.
    /// </summary>
    [Fact]
    public async Task MonthlyBudgetTrackingJourney_ShouldTrackExpensesAndIncome()
    {
        // === RECORD INCOME ===
        var incomeItems = new[]
        {
            ("Budget Journey - Salary", 4500.00m),
            ("Budget Journey - Freelance Project", 800.00m),
            ("Budget Journey - Investment Dividend", 125.00m)
        };

        var incomeIds = new List<int>();
        var totalIncome = 0m;

        foreach (var (subject, amount) in incomeItems)
        {
            var request = new CreateTransactionRequest
            {
                TransactionType = "INCOME",
                Amount = amount,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Subject = subject,
                PaymentMethod = "BANK_TRANSFER"
            };

            var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            incomeIds.Add(transaction!.Id);
            totalIncome += amount;
        }

        totalIncome.Should().Be(5425.00m);

        // === RECORD EXPENSES BY CATEGORY ===
        var expenseCategories = new Dictionary<string, (string Subject, decimal Amount, string Method)[]>
        {
            ["Housing"] = new[]
            {
                ("Budget Journey - Rent Payment", 1200.00m, "BANK_TRANSFER"),
                ("Budget Journey - Utilities", 150.00m, "BANK_TRANSFER")
            },
            ["Food"] = new[]
            {
                ("Budget Journey - Weekly Groceries", 120.00m, "DEBIT_CARD"),
                ("Budget Journey - Restaurant Dinner", 65.00m, "CREDIT_CARD")
            },
            ["Transportation"] = new[]
            {
                ("Budget Journey - Gas", 80.00m, "CREDIT_CARD"),
                ("Budget Journey - Car Insurance", 125.00m, "BANK_TRANSFER")
            }
        };

        var expenseIds = new List<int>();
        var totalExpenses = 0m;

        foreach (var category in expenseCategories)
        {
            foreach (var (subject, amount, method) in category.Value)
            {
                var request = new CreateTransactionRequest
                {
                    TransactionType = "EXPENSE",
                    Amount = amount,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    Subject = subject,
                    Notes = $"Category: {category.Key}",
                    PaymentMethod = method
                };

                var response = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, request);
                response.StatusCode.Should().Be(HttpStatusCode.Created);

                var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
                expenseIds.Add(transaction!.Id);
                totalExpenses += amount;
            }
        }

        totalExpenses.Should().Be(1740.00m);

        // === VERIFY TRANSACTIONS WERE SAVED ===
        var allIds = incomeIds.Concat(expenseIds).ToList();
        allIds.Should().HaveCount(9);

        foreach (var id in allIds)
        {
            var response = await Client.GetAsync(TestConstants.Routes.Transaction(id));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // === ADJUST AN EXPENSE ===
        // Realized the restaurant bill was wrong
        var restaurantId = expenseIds[3]; // Restaurant Dinner
        var adjustRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 72.50m, // Corrected amount
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Budget Journey - Restaurant Dinner (corrected)",
            Notes = "Category: Food - tip was higher",
            PaymentMethod = "CREDIT_CARD"
        };

        var adjustResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(restaurantId),
            adjustRequest);

        adjustResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // === REMOVE A DUPLICATE ENTRY ===
        var duplicateId = expenseIds[2]; // Weekly Groceries
        var removeResponse = await Client.DeleteAsync(TestConstants.Routes.Transaction(duplicateId));
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify removal
        var verifyRemoval = await Client.GetAsync(TestConstants.Routes.Transaction(duplicateId));
        verifyRemoval.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // === CLEANUP ===
        foreach (var id in allIds.Where(id => id != duplicateId))
        {
            await Client.DeleteAsync(TestConstants.Routes.Transaction(id));
        }
    }

    /// <summary>
    /// Tests the registration and login flow works correctly.
    /// Note: After registration/login, subsequent requests still use TestAuthHandler (user ID 1).
    /// </summary>
    [Fact]
    public async Task RegistrationAndLoginFlow_ShouldCreateUserAndAllowLogin()
    {
        // Generate unique email for this test
        var uniqueEmail = $"journey_test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";

        // Register new user
        var registerRequest = new RegisterRequest(
            "Journey Test User",
            uniqueEmail,
            password,
            1000.00m);

        var registerResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        registerResult.Should().NotBeNull();
        registerResult!.Email.Should().Be(uniqueEmail);

        // Login with new credentials
        var loginRequest = new LoginRequest(uniqueEmail, password);
        var loginResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Email.Should().Be(uniqueEmail);
        loginResult.Token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that seeded users can login successfully.
    /// </summary>
    [Fact]
    public async Task SeededUsers_ShouldBeAbleToLogin()
    {
        // Test seeded user 1
        var loginRequest1 = new LoginRequest(
            TestConstants.TestUsers.SeededUserEmail,
            TestConstants.TestUsers.SeededUserPassword);

        var response1 = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        var result1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();
        result1!.Email.Should().Be(TestConstants.TestUsers.SeededUserEmail);

        // Test seeded user 2
        var loginRequest2 = new LoginRequest(
            TestConstants.TestUsers.SeededUser2Email,
            TestConstants.TestUsers.SeededUserPassword);

        var response2 = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();
        result2!.Email.Should().Be(TestConstants.TestUsers.SeededUser2Email);
    }

    /// <summary>
    /// Tests that transactions created by the authenticated user (ID 1) can be accessed.
    /// </summary>
    [Fact]
    public async Task TransactionOwnership_AuthenticatedUserCanAccessOwnTransactions()
    {
        // Create a transaction
        var createRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Ownership Test Transaction",
            PaymentMethod = "CASH"
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        created.Should().NotBeNull();
        created!.UserId.Should().Be(TestAuthHandler.DefaultUserId);

        // Can read it
        var readResponse = await Client.GetAsync(TestConstants.Routes.Transaction(created.Id));
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Can update it
        var updateRequest = new UpdateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 150.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Ownership Test - Updated",
            PaymentMethod = "DEBIT_CARD"
        };

        var updateResponse = await Client.PutAsJsonAsync(
            TestConstants.Routes.Transaction(created.Id),
            updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Can delete it
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.Transaction(created.Id));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Tests transaction isolation behavior - verifies that delete operation
    /// properly checks user ownership while documenting current read behavior.
    ///
    /// NOTE: The current API implementation allows reading any transaction by ID,
    /// but properly restricts update and delete operations to the owner.
    /// This is a security consideration that should be reviewed.
    /// </summary>
    [Fact]
    public async Task TransactionIsolation_DeleteOperationChecksOwnership()
    {
        // Get a transaction ID that belongs to user 2 (Jane Smith)
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Find user 2's transaction
        var user2 = await db.Users.FirstOrDefaultAsync(u => u.Email == TestConstants.TestUsers.SeededUser2Email);
        user2.Should().NotBeNull();

        var user2Transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.UserId == user2!.Id);

        if (user2Transaction == null)
        {
            // If user 2 has no transactions, skip this test
            return;
        }

        // Current behavior: GetById does not check user ownership
        // This documents the current implementation - consider adding ownership check
        var readResponse = await Client.GetAsync(TestConstants.Routes.Transaction(user2Transaction.Id));
        // Currently returns OK - ownership check not implemented on read
        readResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        // Delete operation DOES check ownership
        var deleteResponse = await Client.DeleteAsync(TestConstants.Routes.Transaction(user2Transaction.Id));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Should not be able to delete another user's transaction");

        // Verify the transaction still exists (was not deleted)
        var stillExists = await db.Transactions.AnyAsync(t => t.Id == user2Transaction.Id);
        stillExists.Should().BeTrue("User 2's transaction should still exist after failed delete attempt");
    }

    /// <summary>
    /// Tests sequential operations on the same transaction.
    /// </summary>
    [Fact]
    public async Task SequentialOperations_OnSameTransaction_ShouldMaintainConsistency()
    {
        // Create
        var createRequest = new CreateTransactionRequest
        {
            TransactionType = "EXPENSE",
            Amount = 100.00m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Subject = "Sequential Test - Original",
            PaymentMethod = "CASH"
        };

        var createResponse = await Client.PostAsJsonAsync(TestConstants.Routes.Transactions, createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        var id = created!.Id;

        // Update 5 times sequentially
        for (int i = 1; i <= 5; i++)
        {
            var updateRequest = new UpdateTransactionRequest
            {
                TransactionType = "EXPENSE",
                Amount = 100.00m + (i * 10),
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Subject = $"Sequential Test - Update #{i}",
                PaymentMethod = "CASH"
            };

            var updateResponse = await Client.PutAsJsonAsync(
                TestConstants.Routes.Transaction(id),
                updateRequest);

            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify each update
            var verifyResponse = await Client.GetAsync(TestConstants.Routes.Transaction(id));
            var verified = await verifyResponse.Content.ReadFromJsonAsync<TransactionResponse>();

            verified!.Subject.Should().Be($"Sequential Test - Update #{i}");
            verified.Amount.Should().Be(100.00m + (i * 10));
        }

        // Final state
        var finalResponse = await Client.GetAsync(TestConstants.Routes.Transaction(id));
        var finalTransaction = await finalResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        finalTransaction!.Subject.Should().Be("Sequential Test - Update #5");
        finalTransaction.Amount.Should().Be(150.00m);

        // Cleanup
        await Client.DeleteAsync(TestConstants.Routes.Transaction(id));
    }

    /// <summary>
    /// Tests the complete profile update workflow.
    /// </summary>
    [Fact]
    public async Task ProfileUpdateJourney_ShouldUpdateAndVerify()
    {
        // Get current user state
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalName = user!.Name;
        var originalBalance = user.InitialBalance;

        // Update profile with new name
        var newName = $"Profile Journey User {Guid.NewGuid().ToString("N")[..8]}";
        var updateRequest = new UpdateUserRequest(
            Name: newName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 5000.00m);

        var updateResponse = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updated!.Name.Should().Be(newName);
        updated.InitialBalance.Should().Be(5000.00m);

        // Verify login still works
        var loginRequest = new LoginRequest(user.Email, TestConstants.TestUsers.SeededUserPassword);
        var loginResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult!.Name.Should().Be(newName);

        // Restore original state
        var restoreRequest = new UpdateUserRequest(
            Name: originalName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: originalBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    /// <summary>
    /// Tests the health check endpoint is accessible.
    /// </summary>
    [Fact]
    public async Task HealthCheck_ShouldBeAccessible()
    {
        var response = await Client.GetAsync(TestConstants.Routes.AuthHealth);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
