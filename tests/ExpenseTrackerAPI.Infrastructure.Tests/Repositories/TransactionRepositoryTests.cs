using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Tests.Fixtures;
using ExpenseTrackerAPI.Infrastructure.Transactions;
using ExpenseTrackerAPI.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerAPI.Infrastructure.Tests.Repositories;

[Collection(nameof(DatabaseCollection))]
public class TransactionRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public TransactionRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Rule: when sorted by Date ascending then CreatedAt ascending,
    /// CumulativeDelta[i] = (i == 0 ? 0 : CumulativeDelta[i-1]) + SignedAmount[i].
    /// </summary>
    private static void AssertCumulativeDeltaRule(List<Transaction> chronological)
    {
        Assert.True(chronological.Count >= 1, "Need at least one transaction to verify the rule.");
        decimal previousCumulative = 0;
        for (var i = 0; i < chronological.Count; i++)
        {
            var current = chronological[i];
            var expectedCumulative = previousCumulative + current.SignedAmount;
            Assert.True(current.CumulativeDelta == expectedCumulative,
                $"At index {i} (Date={current.Date}, CreatedAt={current.CreatedAt}): expected CumulativeDelta = {expectedCumulative} (previous {previousCumulative} + signed {current.SignedAmount}), but was {current.CumulativeDelta}.");
            previousCumulative = current.CumulativeDelta;
        }
    }

    private static List<Transaction> ChronologicalForUser(List<Transaction> forUser)
    {
        return forUser
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAt)
            .ToList();
    }

    private async Task<User> CreateTestUserAsync()
    {
        var userRepo = new UserRepository(_fixture.DbContext);
        var user = new User(
            name: "Integration Test User",
            email: $"test-{Guid.NewGuid():N}@integration.test",
            passwordHash: "hashed_password",
            initialBalance: 0
        );
        var result = await userRepo.CreateAsync(user, CancellationToken.None);
        Assert.False(result.IsError);
        return result.Value;
    }

    private static Transaction NewTransaction(int userId, decimal signedAmount, DateOnly date, DateTime createdAt)
    {
        var amount = Math.Abs(signedAmount);
        var transactionType = signedAmount >= 0 ? TransactionType.INCOME : TransactionType.EXPENSE;

        return new Transaction(
            userId: userId,
            transactionType: transactionType,
            amount: amount,
            date: date,
            subject: "Integration test transaction",
            paymentMethod: PaymentMethod.CASH,
            notes: null,
            categoryId: null,
            transactionGroupId: null,
            createdAt: createdAt,
            updatedAt: createdAt
        );
    }

    [Fact]
    public async Task CreateAsync_NewTransactions_CumulativeDeltasFollowRule_ChronologicalOrder()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-10);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var t1 = NewTransaction(user.Id, 100m, day1, baseTime);
        var t2 = NewTransaction(user.Id, -30m, day2, baseTime.AddSeconds(1));
        var t3 = NewTransaction(user.Id, 50m, day3, baseTime.AddSeconds(2));

        // Act
        var r1 = await sut.CreateAsync(t1, CancellationToken.None);
        var r2 = await sut.CreateAsync(t2, CancellationToken.None);
        var r3 = await sut.CreateAsync(t3, CancellationToken.None);

        // Assert
        r1.IsError.Should().BeFalse();
        r2.IsError.Should().BeFalse();
        r3.IsError.Should().BeFalse();

        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        var chronological = ChronologicalForUser(transactions);
        AssertCumulativeDeltaRule(chronological);

        // Verify specific cumulative deltas
        chronological[0].CumulativeDelta.Should().Be(100m);  // 0 + 100
        chronological[1].CumulativeDelta.Should().Be(70m);   // 100 - 30
        chronological[2].CumulativeDelta.Should().Be(120m);  // 70 + 50
    }

    [Fact]
    public async Task CreateAsync_SameDateDifferentCreatedAt_CumulativeDeltasFollowRule()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var sameDay = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        var baseTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 10, 0, 0, DateTimeKind.Utc);

        var t1 = NewTransaction(user.Id, 200m, sameDay, baseTime);
        var t2 = NewTransaction(user.Id, -50m, sameDay, baseTime.AddMinutes(1));
        var t3 = NewTransaction(user.Id, -25m, sameDay, baseTime.AddMinutes(2));

        // Act
        await sut.CreateAsync(t1, CancellationToken.None);
        await sut.CreateAsync(t2, CancellationToken.None);
        await sut.CreateAsync(t3, CancellationToken.None);

        // Assert
        var queryContext = _fixture.CreateNewContext();
        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        var chronological = ChronologicalForUser(transactions);
        AssertCumulativeDeltaRule(chronological);

        chronological[0].CumulativeDelta.Should().Be(200m);   // 0 + 200
        chronological[1].CumulativeDelta.Should().Be(150m);   // 200 - 50
        chronological[2].CumulativeDelta.Should().Be(125m);   // 150 - 25
    }

    [Fact]
    public async Task DeleteAsync_AfterDelete_RemainingTransactionsFollowRule()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-20);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var t1 = await sut.CreateAsync(NewTransaction(user.Id, 100m, day1, baseTime), CancellationToken.None);
        var t2 = await sut.CreateAsync(NewTransaction(user.Id, -40m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        var t3 = await sut.CreateAsync(NewTransaction(user.Id, 60m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        t1.IsError.Should().BeFalse();
        t2.IsError.Should().BeFalse();
        t3.IsError.Should().BeFalse();

        // Verify rule before deletion
        var listBefore = await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(listBefore));

        // Act - Delete middle transaction
        var deleted = await sut.DeleteAsync(t2.Value.Id, CancellationToken.None);

        // Assert
        deleted.IsError.Should().BeFalse();

        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        var chronological = ChronologicalForUser(transactions);
        AssertCumulativeDeltaRule(chronological);

        chronological.Should().HaveCount(2);
        chronological[0].CumulativeDelta.Should().Be(100m);  // 0 + 100
        chronological[1].CumulativeDelta.Should().Be(160m);  // 100 + 60 (middle transaction removed)
    }

    [Fact]
    public async Task UpdateAsync_AmountOnly_AfterUpdate_RuleHeld()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-25);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        await sut.CreateAsync(NewTransaction(user.Id, 100m, day1, baseTime), CancellationToken.None);
        var r2 = await sut.CreateAsync(NewTransaction(user.Id, -20m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        await sut.CreateAsync(NewTransaction(user.Id, 50m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        r2.IsError.Should().BeFalse();
        var oldTransaction = r2.Value;

        var list0 = await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(list0));

        // Act - Update amount
        var updatedTransaction = new Transaction(
            userId: oldTransaction.UserId,
            transactionType: oldTransaction.TransactionType,
            amount: 35m,
            date: oldTransaction.Date,
            subject: oldTransaction.Subject,
            paymentMethod: oldTransaction.PaymentMethod,
            notes: oldTransaction.Notes,
            categoryId: oldTransaction.CategoryId,
            transactionGroupId: oldTransaction.TransactionGroupId,
            createdAt: oldTransaction.CreatedAt,
            updatedAt: DateTime.UtcNow
        );
        updatedTransaction.UpdateId(oldTransaction.Id);

        var result = await sut.UpdateAsync(oldTransaction, updatedTransaction, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();

        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        var chronological = ChronologicalForUser(transactions);
        AssertCumulativeDeltaRule(chronological);

        chronological[0].CumulativeDelta.Should().Be(100m);  // 0 + 100
        chronological[1].CumulativeDelta.Should().Be(65m);   // 100 - 35 (updated from -20)
        chronological[2].CumulativeDelta.Should().Be(115m);  // 65 + 50
    }

    [Fact]
    public async Task UpdateAsync_DateOnly_AfterUpdate_RuleHeld()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-30);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);
        var day4 = day3.AddDays(1);

        await sut.CreateAsync(NewTransaction(user.Id, 80m, day1, baseTime), CancellationToken.None);
        var r2 = await sut.CreateAsync(NewTransaction(user.Id, -25m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        await sut.CreateAsync(NewTransaction(user.Id, 40m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        r2.IsError.Should().BeFalse();
        var oldTransaction = r2.Value;

        AssertCumulativeDeltaRule(ChronologicalForUser((await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync())));

        // Act - Update date
        var updatedTransaction = new Transaction(
            userId: oldTransaction.UserId,
            transactionType: oldTransaction.TransactionType,
            amount: oldTransaction.Amount,
            date: day4,
            subject: oldTransaction.Subject,
            paymentMethod: oldTransaction.PaymentMethod,
            notes: oldTransaction.Notes,
            categoryId: oldTransaction.CategoryId,
            transactionGroupId: oldTransaction.TransactionGroupId,
            createdAt: oldTransaction.CreatedAt,
            updatedAt: DateTime.UtcNow
        );
        updatedTransaction.UpdateId(oldTransaction.Id);

        var updated = await sut.UpdateAsync(oldTransaction, updatedTransaction, CancellationToken.None);

        // Assert
        updated.IsError.Should().BeFalse();
        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        AssertCumulativeDeltaRule(ChronologicalForUser((await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync())));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var transaction = NewTransaction(user.Id, 75.25m, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow);
        var createResult = await sut.CreateAsync(transaction, CancellationToken.None);
        var transactionId = createResult.Value.Id;

        // Act
        var result = await sut.GetByIdAsync(transactionId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(transactionId);
        result.Value.SignedAmount.Should().Be(75.25m);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTransaction_ReturnsError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var nonExistentId = 99999;

        // Act
        var result = await sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Transaction");
    }

    [Fact]
    public async Task CreateAsync_SingleTransaction_FirstCumulativeEqualsSignedAmount()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime).AddDays(-2);
        var transaction = NewTransaction(user.Id, 1000m, day, baseTime);

        // Act
        var created = await sut.CreateAsync(transaction, CancellationToken.None);

        // Assert
        created.IsError.Should().BeFalse();
        created.Value.CumulativeDelta.Should().Be(1000m);
        created.Value.SignedAmount.Should().Be(1000m);

        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(transactions));
    }

    [Fact]
    public async Task CreateAsync_OnlyExpenses_NegativeCumulative_RuleHeld()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-3);
        var day2 = day1.AddDays(1);

        // Act
        await sut.CreateAsync(NewTransaction(user.Id, -100m, day1, baseTime), CancellationToken.None);
        await sut.CreateAsync(NewTransaction(user.Id, -50m, day2, baseTime.AddSeconds(1)), CancellationToken.None);

        // Assert
        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .OrderBy(t => t.Date)
            .ToListAsync();

        AssertCumulativeDeltaRule(ChronologicalForUser(transactions));
        transactions.First().CumulativeDelta.Should().Be(-100m);
        transactions.Last().CumulativeDelta.Should().Be(-150m);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentTransaction_ReturnsError()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var nonExistentId = 99999;

        // Act
        var result = await sut.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Transaction");
    }

    [Fact]
    public async Task DeleteAsync_DeleteFirstTransaction_RemainingRuleHeld()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-12);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var r1 = await sut.CreateAsync(NewTransaction(user.Id, 80m, day1, baseTime), CancellationToken.None);
        await sut.CreateAsync(NewTransaction(user.Id, -20m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        await sut.CreateAsync(NewTransaction(user.Id, 10m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        r1.IsError.Should().BeFalse();

        // Act - Delete first transaction
        var deleted = await sut.DeleteAsync(r1.Value.Id, CancellationToken.None);

        // Assert
        deleted.IsError.Should().BeFalse();

        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        transactions.Should().HaveCount(2);
        var chronological = ChronologicalForUser(transactions);
        AssertCumulativeDeltaRule(chronological);

        chronological[0].CumulativeDelta.Should().Be(-20m);  // First transaction now
        chronological[1].CumulativeDelta.Should().Be(-10m);  // -20 + 10
    }

    [Fact]
    public async Task DeleteAsync_DeleteLastTransaction_RemainingRuleHeld()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-14);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        await sut.CreateAsync(NewTransaction(user.Id, 60m, day1, baseTime), CancellationToken.None);
        await sut.CreateAsync(NewTransaction(user.Id, -15m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        var r3 = await sut.CreateAsync(NewTransaction(user.Id, 5m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        r3.IsError.Should().BeFalse();

        // Act - Delete last transaction
        var deleted = await sut.DeleteAsync(r3.Value.Id, CancellationToken.None);

        // Assert
        deleted.IsError.Should().BeFalse();

        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        transactions.Should().HaveCount(2);
        var chronological = ChronologicalForUser(transactions);
        AssertCumulativeDeltaRule(chronological);

        chronological[0].CumulativeDelta.Should().Be(60m);
        chronological[1].CumulativeDelta.Should().Be(45m);  // Last cumulative delta
    }

    [Fact]
    public async Task CreateThenDeleteAll_UserHasZeroTransactions()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime).AddDays(-1);
        var transaction = NewTransaction(user.Id, 99m, day, baseTime);

        var created = await sut.CreateAsync(transaction, CancellationToken.None);
        created.IsError.Should().BeFalse();

        // Act
        var deleted = await sut.DeleteAsync(created.Value.Id, CancellationToken.None);

        // Assert
        deleted.IsError.Should().BeFalse();

        var transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithDifferentPaymentMethods_ShouldCreateSuccessfully()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime);

        var paymentMethods = new[]
        {
            PaymentMethod.CASH,
            PaymentMethod.CREDIT_CARD,
            PaymentMethod.DEBIT_CARD,
            PaymentMethod.BANK_TRANSFER,
            PaymentMethod.MOBILE_PAYMENT,
            PaymentMethod.OTHER
        };

        // Act & Assert
        foreach (var paymentMethod in paymentMethods)
        {
            var transaction = new Transaction(
                userId: user.Id,
                transactionType: TransactionType.INCOME,
                amount: 10.00m,
                date: day,
                subject: $"Test {paymentMethod}",
                paymentMethod: paymentMethod
            );

            var result = await sut.CreateAsync(transaction, CancellationToken.None);

            result.IsError.Should().BeFalse();
            result.Value.PaymentMethod.Should().Be(paymentMethod);
        }
    }

    [Fact]
    public async Task CreateAsync_WithDifferentUsers_ShouldMaintainSeparateCumulativeDeltas()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user1 = await CreateTestUserAsync();
        var user2 = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime);

        // Act
        var transaction1 = NewTransaction(user1.Id, 500.00m, day, baseTime);
        var result1 = await sut.CreateAsync(transaction1, CancellationToken.None);

        var transaction2 = NewTransaction(user2.Id, 300.00m, day, baseTime);
        var result2 = await sut.CreateAsync(transaction2, CancellationToken.None);

        // Assert
        result1.IsError.Should().BeFalse();
        result1.Value.CumulativeDelta.Should().Be(500.00m);

        result2.IsError.Should().BeFalse();
        result2.Value.CumulativeDelta.Should().Be(300.00m);

        // Verify each user has their own transactions
        var user1Transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user1.Id)
            .ToListAsync();
        user1Transactions.Should().HaveCount(1);

        var user2Transactions = await _fixture.DbContext.Transactions
            .Where(t => t.UserId == user2.Id)
            .ToListAsync();
        user2Transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_Update_Delete_Combine_RuleAlwaysHeld()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        var user = await CreateTestUserAsync();
        var sut = new TransactionRepository(_fixture.DbContext);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-45);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var r1 = await sut.CreateAsync(NewTransaction(user.Id, 150m, day1, baseTime), CancellationToken.None);
        var r2 = await sut.CreateAsync(NewTransaction(user.Id, -50m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        var r3 = await sut.CreateAsync(NewTransaction(user.Id, 30m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        r1.IsError.Should().BeFalse();
        r2.IsError.Should().BeFalse();
        r3.IsError.Should().BeFalse();

        // Verify initial state
        var transactions1 = await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(transactions1));

        // Act - Update middle transaction
        var oldTransaction = r2.Value;
        var updatedTransaction = new Transaction(
            userId: oldTransaction.UserId,
            transactionType: oldTransaction.TransactionType,
            amount: 40m,
            date: oldTransaction.Date,
            subject: oldTransaction.Subject,
            paymentMethod: oldTransaction.PaymentMethod,
            notes: oldTransaction.Notes,
            categoryId: oldTransaction.CategoryId,
            transactionGroupId: oldTransaction.TransactionGroupId,
            createdAt: oldTransaction.CreatedAt,
            updatedAt: DateTime.UtcNow
        );
        updatedTransaction.UpdateId(oldTransaction.Id);

        var updateResult = await sut.UpdateAsync(oldTransaction, updatedTransaction, CancellationToken.None);
        updateResult.IsError.Should().BeFalse();

        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        var transactions2 = await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(transactions2));

        // Act - Delete last transaction
        var deleteResult = await sut.DeleteAsync(r3.Value.Id, CancellationToken.None);
        deleteResult.IsError.Should().BeFalse();

        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        var transactions3 = await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(transactions3));

        // Act - Create new transaction
        await sut.CreateAsync(NewTransaction(user.Id, 25m, day3.AddDays(1), baseTime.AddSeconds(3)), CancellationToken.None);

        // Final verification
        // Clear change tracker to get fresh data from database
        _fixture.DbContext.ChangeTracker.Clear();
        var transactionsFinal = await _fixture.DbContext.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
        AssertCumulativeDeltaRule(ChronologicalForUser(transactionsFinal));
    }
}
