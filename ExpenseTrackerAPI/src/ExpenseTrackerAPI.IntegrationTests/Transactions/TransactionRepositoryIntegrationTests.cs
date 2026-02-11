using ErrorOr;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ExpenseTrackerAPI.IntegrationTests;

public class TransactionRepositoryIntegrationTests : IClassFixture<TransactionDatabaseFixture>
{
    private readonly TransactionDatabaseFixture _fixture;

    public TransactionRepositoryIntegrationTests(TransactionDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Rule: when sorted by date ascending then CreatedAt ascending, cumulativeDelta[i] = (i == 0 ? 0 : cumulativeDelta[i-1]) + signedAmount[i].
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

    /// <summary>BCrypt hash for "password123" (matches seed).</summary>
    private const string TestUserPasswordHash = "$2a$11$yQRQSx3N6m00FZPwo/uQiOhMyxf/pKAtSiijU6EoXKQtrGv5WvNF.";

    private static async Task<int> CreateTestUserAsync(IUserRepository userRepo, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = "Integration Test User",
            Email = $"test-{Guid.NewGuid():N}@integration.test",
            PasswordHash = TestUserPasswordHash,
            InitialBalance = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        var result = await userRepo.CreateUserAsync(user, cancellationToken);
        Assert.False(result.IsError);
        return result.Value.Id;
    }

    [Fact]
    public async Task GetByUserIdAsync_SeededUser_ReturnsTransactions()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        var result = await repo.GetByUserIdAsync(1, CancellationToken.None);

        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.True(transactions.Count >= 1);
        Assert.All(transactions, t => Assert.Equal(1, t.UserId));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTransaction_ReturnsTransaction()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        var listResult = await repo.GetByUserIdAsync(1, CancellationToken.None);
        Assert.False(listResult.IsError);
        var firstId = listResult.Value.First().Id;

        var result = await repo.GetByIdAsync(firstId, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(firstId, result.Value.Id);
        Assert.Equal(1, result.Value.UserId);
    }

    [Fact]
    public async Task GetByUserIdAndDateRangeAsync_ValidRange_ReturnsTransactionsInRange()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-60);
        var end = today.AddDays(1);

        var result = await repo.GetByUserIdAndDateRangeAsync(1, start, end, CancellationToken.None);

        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.All(transactions, t =>
        {
            Assert.True(t.Date >= start && t.Date <= end);
            Assert.Equal(1, t.UserId);
        });
    }

    [Fact]
    public async Task GetByUserIdAndTypeAsync_Income_ReturnsOnlyIncome()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        var result = await repo.GetByUserIdAndTypeAsync(1, TransactionType.INCOME, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.All(result.Value, t => Assert.Equal(TransactionType.INCOME, t.TransactionType));
    }

    [Fact]
    public async Task CreateAsync_NewTransactions_CumulativeDeltasFollowRule_ChronologicalOrder()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-10);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var t1 = NewTransaction(userId, 100m, day1, baseTime);
        var t2 = NewTransaction(userId, -30m, day2, baseTime.AddSeconds(1));
        var t3 = NewTransaction(userId, 50m, day3, baseTime.AddSeconds(2));

        var r1 = await repo.CreateAsync(t1, CancellationToken.None);
        Assert.False(r1.IsError);
        var r2 = await repo.CreateAsync(t2, CancellationToken.None);
        Assert.False(r2.IsError);
        var r3 = await repo.CreateAsync(t3, CancellationToken.None);
        Assert.False(r3.IsError);

        var list = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(list.IsError);
        var chronological = ChronologicalForUser(list.Value);
        AssertCumulativeDeltaRule(chronological);
    }

    [Fact]
    public async Task CreateAsync_SameDateDifferentCreatedAt_CumulativeDeltasFollowRule()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var sameDay = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        var baseTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 10, 0, 0, DateTimeKind.Utc);

        var t1 = NewTransaction(userId, 200m, sameDay, baseTime);
        var t2 = NewTransaction(userId, -50m, sameDay, baseTime.AddMinutes(1));
        var t3 = NewTransaction(userId, -25m, sameDay, baseTime.AddMinutes(2));

        await repo.CreateAsync(t1, CancellationToken.None);
        await repo.CreateAsync(t2, CancellationToken.None);
        await repo.CreateAsync(t3, CancellationToken.None);

        var list = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(list.IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser(list.Value));
    }

    [Fact]
    public async Task DeleteAsync_AfterDelete_RemainingTransactionsFollowRule()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-20);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var t1 = await repo.CreateAsync(NewTransaction(userId, 100m, day1, baseTime), CancellationToken.None);
        Assert.False(t1.IsError);
        var t2 = await repo.CreateAsync(NewTransaction(userId, -40m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(t2.IsError);
        var t3 = await repo.CreateAsync(NewTransaction(userId, 60m, day3, baseTime.AddSeconds(2)), CancellationToken.None);
        Assert.False(t3.IsError);

        var listBefore = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(listBefore.IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser(listBefore.Value));

        var deleted = await repo.DeleteAsync(t2.Value.Id, CancellationToken.None);
        Assert.False(deleted.IsError);

        var listAfter = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(listAfter.IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser(listAfter.Value));
    }

    [Fact]
    public async Task UpdateAsync_AmountOnly_AfterUpdate_RuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-25);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, 100m, day1, baseTime), CancellationToken.None);
        var r2 = await repo.CreateAsync(NewTransaction(userId, -20m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(r2.IsError);
        await repo.CreateAsync(NewTransaction(userId, 50m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        var list0 = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(list0.IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser(list0.Value));

        var toUpdate = r2.Value;
        toUpdate.Amount = 35m;
        toUpdate.SignedAmount = -35m;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(toUpdate, CancellationToken.None);
        Assert.False(updated.IsError);

        var list1 = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(list1.IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser(list1.Value));
    }

    [Fact]
    public async Task UpdateAsync_DateOnly_AfterUpdate_RuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-30);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);
        var day4 = day3.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, 80m, day1, baseTime), CancellationToken.None);
        var r2 = await repo.CreateAsync(NewTransaction(userId, -25m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(r2.IsError);
        await repo.CreateAsync(NewTransaction(userId, 40m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));

        var toUpdate = r2.Value;
        toUpdate.Date = day4;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(toUpdate, CancellationToken.None);
        Assert.False(updated.IsError);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));
    }

    [Fact]
    public async Task UpdateAsync_DateMovedToExistingDate_OrderByCreatedAt_RuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var dayA = DateOnly.FromDateTime(baseTime).AddDays(-35);
        var dayB = dayA.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, 100m, dayA, baseTime), CancellationToken.None);
        await repo.CreateAsync(NewTransaction(userId, -30m, dayA, baseTime.AddMinutes(1)), CancellationToken.None);
        var onDayB = await repo.CreateAsync(NewTransaction(userId, 50m, dayB, baseTime.AddMinutes(2)), CancellationToken.None);
        Assert.False(onDayB.IsError);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));

        var toUpdate = onDayB.Value;
        toUpdate.Date = dayA;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(toUpdate, CancellationToken.None);
        Assert.False(updated.IsError);

        var chronological = ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value);
        AssertCumulativeDeltaRule(chronological);
        Assert.Equal(3, chronological.Count(t => t.Date == dayA));
    }

    [Fact]
    public async Task UpdateAsync_DateAndAmount_RuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-40);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);
        var dayNew = day1.AddDays(-1);

        await repo.CreateAsync(NewTransaction(userId, 200m, day1, baseTime), CancellationToken.None);
        var r2 = await repo.CreateAsync(NewTransaction(userId, -60m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(r2.IsError);
        await repo.CreateAsync(NewTransaction(userId, 80m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));

        var toUpdate = r2.Value;
        toUpdate.Date = dayNew;
        toUpdate.Amount = 90m;
        toUpdate.SignedAmount = -90m;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(toUpdate, CancellationToken.None);
        Assert.False(updated.IsError);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));
    }

    [Fact]
    public async Task Create_Update_Delete_Combine_RuleAlwaysHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-45);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var r1 = await repo.CreateAsync(NewTransaction(userId, 150m, day1, baseTime), CancellationToken.None);
        Assert.False(r1.IsError);
        var r2 = await repo.CreateAsync(NewTransaction(userId, -50m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(r2.IsError);
        var r3 = await repo.CreateAsync(NewTransaction(userId, 30m, day3, baseTime.AddSeconds(2)), CancellationToken.None);
        Assert.False(r3.IsError);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));

        var toUpdate = r2.Value;
        toUpdate.SignedAmount = -40m;
        toUpdate.Amount = 40m;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        Assert.False((await repo.UpdateAsync(toUpdate, CancellationToken.None)).IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));

        Assert.False((await repo.DeleteAsync(r3.Value.Id, CancellationToken.None)).IsError);
        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));

        await repo.CreateAsync(NewTransaction(userId, 25m, day3.AddDays(1), baseTime.AddSeconds(3)), CancellationToken.None);
        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));
    }

    [Fact]
    public async Task GetByUserIdAsync_UserWithNoTransactions_ReturnsEmptyList()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);

        var result = await repo.GetByUserIdAsync(userId, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNotFound()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        var result = await repo.GetByIdAsync(999999, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Type == ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsNotFound()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

        var result = await repo.DeleteAsync(999999, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Type == ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAndDateRangeAsync_RangeWithNoTransactions_ReturnsEmptyList()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-100);
        var end = today.AddDays(-90);

        var result = await repo.GetByUserIdAndDateRangeAsync(userId, start, end, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetByUserIdAndTypeAsync_NoMatchingType_ReturnsEmptyList()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime).AddDays(-1);
        await repo.CreateAsync(NewTransaction(userId, -50m, day, baseTime), CancellationToken.None);

        var result = await repo.GetByUserIdAndTypeAsync(userId, TransactionType.INCOME, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task CreateAsync_SingleTransaction_FirstCumulativeEqualsSignedAmount()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime).AddDays(-2);

        var created = await repo.CreateAsync(NewTransaction(userId, 1000m, day, baseTime), CancellationToken.None);

        Assert.False(created.IsError);
        Assert.Equal(1000m, created.Value.CumulativeDelta);
        Assert.Equal(1000m, created.Value.SignedAmount);
        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));
    }

    [Fact]
    public async Task CreateAsync_OnlyExpenses_NegativeCumulative_RuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-3);
        var day2 = day1.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, -100m, day1, baseTime), CancellationToken.None);
        await repo.CreateAsync(NewTransaction(userId, -50m, day2, baseTime.AddSeconds(1)), CancellationToken.None);

        var list = (await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value;
        AssertCumulativeDeltaRule(ChronologicalForUser(list));
        Assert.Equal(-100m, list.OrderBy(t => t.Date).First().CumulativeDelta);
        Assert.Equal(-150m, list.OrderBy(t => t.Date).Last().CumulativeDelta);
    }

    [Fact]
    public async Task UpdateAsync_NoOpSameAmountAndDate_RuleStillHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-8);
        var day2 = day1.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, 200m, day1, baseTime), CancellationToken.None);
        var r2 = await repo.CreateAsync(NewTransaction(userId, -75m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(r2.IsError);

        var toUpdate = r2.Value;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(toUpdate, CancellationToken.None);
        Assert.False(updated.IsError);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));
    }

    [Fact]
    public async Task DeleteAsync_DeleteFirstTransaction_RemainingRuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-12);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        var r1 = await repo.CreateAsync(NewTransaction(userId, 80m, day1, baseTime), CancellationToken.None);
        Assert.False(r1.IsError);
        await repo.CreateAsync(NewTransaction(userId, -20m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        await repo.CreateAsync(NewTransaction(userId, 10m, day3, baseTime.AddSeconds(2)), CancellationToken.None);

        var deleted = await repo.DeleteAsync(r1.Value.Id, CancellationToken.None);
        Assert.False(deleted.IsError);

        var list = (await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value;
        Assert.Equal(2, list.Count);
        AssertCumulativeDeltaRule(ChronologicalForUser(list));
    }

    [Fact]
    public async Task DeleteAsync_DeleteLastTransaction_RemainingRuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-14);
        var day2 = day1.AddDays(1);
        var day3 = day2.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, 60m, day1, baseTime), CancellationToken.None);
        await repo.CreateAsync(NewTransaction(userId, -15m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        var r3 = await repo.CreateAsync(NewTransaction(userId, 5m, day3, baseTime.AddSeconds(2)), CancellationToken.None);
        Assert.False(r3.IsError);

        var deleted = await repo.DeleteAsync(r3.Value.Id, CancellationToken.None);
        Assert.False(deleted.IsError);

        var list = (await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value;
        Assert.Equal(2, list.Count);
        AssertCumulativeDeltaRule(ChronologicalForUser(list));
    }

    [Fact]
    public async Task CreateThenDeleteAll_UserHasZeroTransactions()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day = DateOnly.FromDateTime(baseTime).AddDays(-1);

        var r = await repo.CreateAsync(NewTransaction(userId, 99m, day, baseTime), CancellationToken.None);
        Assert.False(r.IsError);

        var deleted = await repo.DeleteAsync(r.Value.Id, CancellationToken.None);
        Assert.False(deleted.IsError);

        var list = await repo.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.False(list.IsError);
        Assert.Empty(list.Value);
    }

    [Fact]
    public async Task UpdateAsync_AmountToZero_SubsequentUnchanged_RuleHeld()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var repo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var userId = await CreateTestUserAsync(userRepo);
        var baseTime = DateTime.UtcNow;
        var day1 = DateOnly.FromDateTime(baseTime).AddDays(-18);
        var day2 = day1.AddDays(1);

        await repo.CreateAsync(NewTransaction(userId, 100m, day1, baseTime), CancellationToken.None);
        var r2 = await repo.CreateAsync(NewTransaction(userId, -30m, day2, baseTime.AddSeconds(1)), CancellationToken.None);
        Assert.False(r2.IsError);

        var toUpdate = r2.Value;
        toUpdate.Amount = 0m;
        toUpdate.SignedAmount = 0m;
        toUpdate.UpdatedAt = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(toUpdate, CancellationToken.None);
        Assert.False(updated.IsError);

        AssertCumulativeDeltaRule(ChronologicalForUser((await repo.GetByUserIdAsync(userId, CancellationToken.None)).Value));
    }

    private static Transaction NewTransaction(int userId, decimal signedAmount, DateOnly date, DateTime createdAt)
    {
        var amount = decimal.Abs(signedAmount);
        return new Transaction
        {
            UserId = userId,
            TransactionType = signedAmount >= 0 ? TransactionType.INCOME : TransactionType.EXPENSE,
            Amount = amount,
            SignedAmount = signedAmount,
            Date = date,
            Subject = "Integration test",
            PaymentMethod = PaymentMethod.CASH,
            CategoryId = signedAmount < 0 ? 1 : null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
