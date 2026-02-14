using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Categories;
using ExpenseTrackerAPI.Infrastructure.Tests.Fixtures;
using ExpenseTrackerAPI.Infrastructure.TransactionGroups;
using ExpenseTrackerAPI.Infrastructure.Transactions;
using ExpenseTrackerAPI.Infrastructure.Users;

namespace ExpenseTrackerAPI.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for TransactionRepository filtering functionality.
/// Tests the GetByUserIdWithFilterAsync method and its filter/sort logic.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class TransactionRepositoryFilteringTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private TransactionRepository _repository = null!;
    private User _testUser = null!;
    private Category _testCategory1 = null!;
    private Category _testCategory2 = null!;
    private TransactionGroup _testGroup1 = null!;
    private TransactionGroup _testGroup2 = null!;

    public TransactionRepositoryFilteringTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _repository = new TransactionRepository(_fixture.DbContext);

        // Create test user
        var userRepo = new UserRepository(_fixture.DbContext);
        var userResult = await userRepo.CreateAsync(new User(
            name: "Filter Test User",
            email: $"filtertest-{Guid.NewGuid():N}@test.com",
            passwordHash: "hashed_password",
            initialBalance: 0
        ), CancellationToken.None);
        _testUser = userResult.Value;

        // Create test categories
        _testCategory1 = new Category
        {
            Name = "Food",
            Icon = "🍔",
            Description = null
        };
        _fixture.DbContext.Categories.Add(_testCategory1);
        await _fixture.DbContext.SaveChangesAsync();

        _testCategory2 = new Category
        {
            Name = "Transport",
            Icon = "🚗",
            Description = null
        };
        _fixture.DbContext.Categories.Add(_testCategory2);
        await _fixture.DbContext.SaveChangesAsync();

        // Create test transaction groups
        _testGroup1 = new TransactionGroup
        {
            Name = "Monthly Bills",
            Description = null,
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        _fixture.DbContext.TransactionGroups.Add(_testGroup1);
        await _fixture.DbContext.SaveChangesAsync();

        _testGroup2 = new TransactionGroup
        {
            Name = "Vacation Expenses",
            Description = null,
            UserId = _testUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        _fixture.DbContext.TransactionGroups.Add(_testGroup2);
        await _fixture.DbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Helper Methods

    private async Task<Transaction> CreateTransactionAsync(
        TransactionType type,
        decimal amount,
        DateOnly date,
        string subject,
        PaymentMethod paymentMethod = PaymentMethod.CASH,
        string? notes = null,
        int? categoryId = null,
        int? transactionGroupId = null,
        DateTime? createdAt = null)
    {
        var transaction = new Transaction(
            userId: _testUser.Id,
            transactionType: type,
            amount: amount,
            date: date,
            subject: subject,
            paymentMethod: paymentMethod,
            notes: notes,
            categoryId: categoryId,
            transactionGroupId: transactionGroupId,
            createdAt: createdAt ?? DateTime.UtcNow,
            updatedAt: createdAt ?? DateTime.UtcNow
        );

        var result = await _repository.CreateAsync(transaction, CancellationToken.None);
        Assert.False(result.IsError);
        return result.Value;
    }

    private static TransactionFilter CreateFilter(
        TransactionType? transactionType = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? subjectContains = null,
        string? notesContains = null,
        IReadOnlyList<PaymentMethod>? paymentMethods = null,
        IReadOnlyList<int>? categoryIds = null,
        bool uncategorized = false,
        IReadOnlyList<int>? transactionGroupIds = null,
        bool ungrouped = false,
        TransactionSortField sortBy = TransactionSortField.Date,
        bool sortDescending = true,
        int page = 1,
        int pageSize = 20)
    {
        return new TransactionFilter
        {
            TransactionType = transactionType,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SubjectContains = subjectContains,
            NotesContains = notesContains,
            PaymentMethods = paymentMethods,
            CategoryIds = categoryIds,
            Uncategorized = uncategorized,
            TransactionGroupIds = transactionGroupIds,
            Ungrouped = ungrouped,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize
        };
    }

    #endregion

    #region Transaction Type Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByExpenseType_ReturnsOnlyExpenses()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Expense 1");
        await CreateTransactionAsync(TransactionType.EXPENSE, 75m, new DateOnly(2024, 1, 2), "Expense 2");
        await CreateTransactionAsync(TransactionType.INCOME, 100m, new DateOnly(2024, 1, 3), "Income 1");
        await CreateTransactionAsync(TransactionType.INCOME, 200m, new DateOnly(2024, 1, 4), "Income 2");

        var filter = CreateFilter(transactionType: TransactionType.EXPENSE, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(TransactionType.EXPENSE, t.TransactionType));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByIncomeType_ReturnsOnlyIncome()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Expense 1");
        await CreateTransactionAsync(TransactionType.INCOME, 100m, new DateOnly(2024, 1, 2), "Income 1");
        await CreateTransactionAsync(TransactionType.INCOME, 200m, new DateOnly(2024, 1, 3), "Income 2");

        var filter = CreateFilter(transactionType: TransactionType.INCOME, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(TransactionType.INCOME, t.TransactionType));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_NoTypeFilter_ReturnsAllTypes()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Expense 1");
        await CreateTransactionAsync(TransactionType.INCOME, 100m, new DateOnly(2024, 1, 2), "Income 1");

        var filter = CreateFilter(pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
    }

    #endregion

    #region Amount Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByMinAmount_ReturnsTransactionsAboveMinimum()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 25m, new DateOnly(2024, 1, 1), "Low amount");
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 2), "Mid amount");
        await CreateTransactionAsync(TransactionType.EXPENSE, 100m, new DateOnly(2024, 1, 3), "High amount");

        var filter = CreateFilter(minAmount: 50m, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(t.Amount >= 50m));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByMaxAmount_ReturnsTransactionsBelowMaximum()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 25m, new DateOnly(2024, 1, 1), "Low amount");
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 2), "Mid amount");
        await CreateTransactionAsync(TransactionType.EXPENSE, 100m, new DateOnly(2024, 1, 3), "High amount");

        var filter = CreateFilter(maxAmount: 50m, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(t.Amount <= 50m));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByAmountRange_ReturnsTransactionsInRange()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 25m, new DateOnly(2024, 1, 1), "Too low");
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 2), "In range 1");
        await CreateTransactionAsync(TransactionType.EXPENSE, 75m, new DateOnly(2024, 1, 3), "In range 2");
        await CreateTransactionAsync(TransactionType.EXPENSE, 100m, new DateOnly(2024, 1, 4), "In range 3");
        await CreateTransactionAsync(TransactionType.EXPENSE, 150m, new DateOnly(2024, 1, 5), "Too high");

        var filter = CreateFilter(minAmount: 50m, maxAmount: 100m, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, t => Assert.True(t.Amount >= 50m && t.Amount <= 100m));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByExactAmount_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Exact 1");
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 2), "Exact 2");
        await CreateTransactionAsync(TransactionType.EXPENSE, 75m, new DateOnly(2024, 1, 3), "Different");

        var filter = CreateFilter(minAmount: 50m, maxAmount: 50m, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(50m, t.Amount));
    }

    #endregion

    #region Date Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByDateFrom_ReturnsTransactionsFromDate()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Before");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "On date");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 10), "After");

        var filter = CreateFilter(dateFrom: new DateOnly(2024, 1, 5), pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(t.Date >= new DateOnly(2024, 1, 5)));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByDateTo_ReturnsTransactionsUpToDate()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Before");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "On date");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 10), "After");

        var filter = CreateFilter(dateTo: new DateOnly(2024, 1, 5), pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(t.Date <= new DateOnly(2024, 1, 5)));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByDateRange_ReturnsTransactionsInRange()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Before range");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "Start of range");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 7), "Middle of range");
        await CreateTransactionAsync(TransactionType.EXPENSE, 80m, new DateOnly(2024, 1, 10), "End of range");
        await CreateTransactionAsync(TransactionType.EXPENSE, 90m, new DateOnly(2024, 1, 15), "After range");

        var filter = CreateFilter(
            dateFrom: new DateOnly(2024, 1, 5),
            dateTo: new DateOnly(2024, 1, 10),
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, t => Assert.True(
            t.Date >= new DateOnly(2024, 1, 5) && t.Date <= new DateOnly(2024, 1, 10)));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterBySingleDate_ReturnsTransactionsOnThatDate()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 4), "Day before");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "Target day 1");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 5), "Target day 2");
        await CreateTransactionAsync(TransactionType.EXPENSE, 80m, new DateOnly(2024, 1, 6), "Day after");

        var targetDate = new DateOnly(2024, 1, 5);
        var filter = CreateFilter(dateFrom: targetDate, dateTo: targetDate, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(targetDate, t.Date));
    }

    #endregion

    #region Subject and Notes Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterBySubjectContains_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Grocery Shopping");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Gas Station");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Shopping Mall");

        var filter = CreateFilter(subjectContains: "shopping", pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Contains("shopping", t.Subject, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterBySubjectContains_IsCaseInsensitive()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "COFFEE Shop");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "coffee break");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Tea House");

        var filter = CreateFilter(subjectContains: "CoFfEe", pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByNotesContains_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Purchase 1", notes: "urgent payment");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Purchase 2", notes: "regular payment");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Purchase 3", notes: "invoice received");

        var filter = CreateFilter(notesContains: "payment", pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Contains("payment", t.Notes ?? "", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByNotesContains_ExcludesNullNotes()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "With notes", notes: "important");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Without notes", notes: null);

        var filter = CreateFilter(notesContains: "important", pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.NotNull(transactions[0].Notes);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByBothSubjectAndNotes_ReturnsOnlyMatchingBoth()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Coffee shop", notes: "urgent");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Coffee house", notes: "regular");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Tea shop", notes: "urgent");

        var filter = CreateFilter(subjectContains: "coffee", notesContains: "urgent", pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Contains("coffee", transactions[0].Subject, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("urgent", transactions[0].Notes ?? "", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Payment Method Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterBySinglePaymentMethod_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Cash payment", PaymentMethod.CASH);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Card payment", PaymentMethod.DEBIT_CARD);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Another cash", PaymentMethod.CASH);

        var filter = CreateFilter(paymentMethods: new[] { PaymentMethod.CASH }, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(PaymentMethod.CASH, t.PaymentMethod));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByMultiplePaymentMethods_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Cash", PaymentMethod.CASH);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Debit", PaymentMethod.DEBIT_CARD);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Credit", PaymentMethod.CREDIT_CARD);
        await CreateTransactionAsync(TransactionType.EXPENSE, 80m, new DateOnly(2024, 1, 4), "Bank", PaymentMethod.BANK_TRANSFER);

        var filter = CreateFilter(
            paymentMethods: new[] { PaymentMethod.CASH, PaymentMethod.DEBIT_CARD, PaymentMethod.CREDIT_CARD },
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, t => Assert.Contains(t.PaymentMethod,
            new[] { PaymentMethod.CASH, PaymentMethod.DEBIT_CARD, PaymentMethod.CREDIT_CARD }));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_EmptyPaymentMethodsList_ReturnsAllTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Cash", PaymentMethod.CASH);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Card", PaymentMethod.DEBIT_CARD);

        var filter = CreateFilter(paymentMethods: Array.Empty<PaymentMethod>(), pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
    }

    #endregion

    #region Category Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterBySingleCategory_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Food", categoryId: _testCategory1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Transport", categoryId: _testCategory2.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "More food", categoryId: _testCategory1.Id);

        var filter = CreateFilter(categoryIds: new[] { _testCategory1.Id }, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(_testCategory1.Id, t.CategoryId));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByMultipleCategories_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Food", categoryId: _testCategory1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Transport", categoryId: _testCategory2.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Uncategorized", categoryId: null);

        var filter = CreateFilter(categoryIds: new[] { _testCategory1.Id, _testCategory2.Id }, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(
            t.CategoryId == _testCategory1.Id || t.CategoryId == _testCategory2.Id));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByUncategorized_ReturnsOnlyUncategorizedTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Food", categoryId: _testCategory1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Uncategorized 1", categoryId: null);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Uncategorized 2", categoryId: null);

        var filter = CreateFilter(uncategorized: true, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Null(t.CategoryId));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_EmptyCategoryIdsList_ReturnsAllTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Food", categoryId: _testCategory1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Transport", categoryId: _testCategory2.Id);

        var filter = CreateFilter(categoryIds: Array.Empty<int>(), pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
    }

    #endregion

    #region Transaction Group Filtering

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterBySingleGroup_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Bill 1", transactionGroupId: _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Vacation 1", transactionGroupId: _testGroup2.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Bill 2", transactionGroupId: _testGroup1.Id);

        var filter = CreateFilter(transactionGroupIds: new[] { _testGroup1.Id }, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(_testGroup1.Id, t.TransactionGroupId));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByMultipleGroups_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Bill", transactionGroupId: _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Vacation", transactionGroupId: _testGroup2.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Ungrouped", transactionGroupId: null);

        var filter = CreateFilter(transactionGroupIds: new[] { _testGroup1.Id, _testGroup2.Id }, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(
            t.TransactionGroupId == _testGroup1.Id || t.TransactionGroupId == _testGroup2.Id));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_FilterByUngrouped_ReturnsOnlyUngroupedTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Grouped", transactionGroupId: _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Ungrouped 1", transactionGroupId: null);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Ungrouped 2", transactionGroupId: null);

        var filter = CreateFilter(ungrouped: true, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.Null(t.TransactionGroupId));
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_EmptyTransactionGroupIdsList_ReturnsAllTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Grouped 1", transactionGroupId: _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Grouped 2", transactionGroupId: _testGroup2.Id);

        var filter = CreateFilter(transactionGroupIds: Array.Empty<int>(), pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByDateAscending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 10), "Latest");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "Middle");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 1), "Earliest");

        var filter = CreateFilter(sortBy: TransactionSortField.Date, sortDescending: false, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal(new DateOnly(2024, 1, 1), transactions[0].Date);
        Assert.Equal(new DateOnly(2024, 1, 5), transactions[1].Date);
        Assert.Equal(new DateOnly(2024, 1, 10), transactions[2].Date);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByDateDescending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Earliest");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "Middle");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 10), "Latest");

        var filter = CreateFilter(sortBy: TransactionSortField.Date, sortDescending: true, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal(new DateOnly(2024, 1, 10), transactions[0].Date);
        Assert.Equal(new DateOnly(2024, 1, 5), transactions[1].Date);
        Assert.Equal(new DateOnly(2024, 1, 1), transactions[2].Date);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByAmountAscending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 100m, new DateOnly(2024, 1, 1), "Highest");
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 2), "Middle");
        await CreateTransactionAsync(TransactionType.EXPENSE, 25m, new DateOnly(2024, 1, 3), "Lowest");

        var filter = CreateFilter(sortBy: TransactionSortField.Amount, sortDescending: false, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal(25m, transactions[0].Amount);
        Assert.Equal(50m, transactions[1].Amount);
        Assert.Equal(100m, transactions[2].Amount);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByAmountDescending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 25m, new DateOnly(2024, 1, 1), "Lowest");
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 2), "Middle");
        await CreateTransactionAsync(TransactionType.EXPENSE, 100m, new DateOnly(2024, 1, 3), "Highest");

        var filter = CreateFilter(sortBy: TransactionSortField.Amount, sortDescending: true, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal(100m, transactions[0].Amount);
        Assert.Equal(50m, transactions[1].Amount);
        Assert.Equal(25m, transactions[2].Amount);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortBySubjectAscending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Zebra");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Apple");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Mango");

        var filter = CreateFilter(sortBy: TransactionSortField.Subject, sortDescending: false, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal("Apple", transactions[0].Subject);
        Assert.Equal("Mango", transactions[1].Subject);
        Assert.Equal("Zebra", transactions[2].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByCreatedAtAscending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange - create with explicit CreatedAt timestamps
        var baseTime = DateTime.UtcNow.AddDays(-10);
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Third", createdAt: baseTime.AddHours(3));
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 1), "First", createdAt: baseTime.AddHours(1));
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 1), "Second", createdAt: baseTime.AddHours(2));

        var filter = CreateFilter(sortBy: TransactionSortField.CreatedAt, sortDescending: false, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal("First", transactions[0].Subject);
        Assert.Equal("Second", transactions[1].Subject);
        Assert.Equal("Third", transactions[2].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByUpdatedAtDescending_ReturnsTransactionsInCorrectOrder()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(-10);
        var t1 = await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Oldest update", createdAt: baseTime);
        var t2 = await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Middle update", createdAt: baseTime);
        var t3 = await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Newest update", createdAt: baseTime);

        // Update t3 to have the most recent UpdatedAt
        await Task.Delay(10);
        t3.UpdateCumulativeDelta(t3.CumulativeDelta); // This updates UpdatedAt
        await _fixture.DbContext.SaveChangesAsync();

        var filter = CreateFilter(sortBy: TransactionSortField.UpdatedAt, sortDescending: true, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        Assert.Equal("Newest update", transactions[0].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_SortByDate_ThenByCreatedAt_AsSecondarySort()
    {
        // Arrange - same date, different creation times
        var baseTime = DateTime.UtcNow.AddDays(-10);
        var sameDate = new DateOnly(2024, 1, 5);
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, sameDate, "Third", createdAt: baseTime.AddHours(3));
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, sameDate, "First", createdAt: baseTime.AddHours(1));
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, sameDate, "Second", createdAt: baseTime.AddHours(2));

        var filter = CreateFilter(sortBy: TransactionSortField.Date, sortDescending: false, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        // All same date, so should be sorted by CreatedAt ascending
        Assert.Equal("First", transactions[0].Subject);
        Assert.Equal("Second", transactions[1].Subject);
        Assert.Equal("Third", transactions[2].Subject);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetByUserIdWithFilterAsync_Pagination_FirstPage_ReturnsCorrectTransactions()
    {
        // Arrange - create 5 transactions
        for (int i = 1; i <= 5; i++)
        {
            await CreateTransactionAsync(TransactionType.EXPENSE, i * 10m, new DateOnly(2024, 1, i), $"Transaction {i}");
        }

        var filter = CreateFilter(page: 1, pageSize: 2, sortDescending: false);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.Equal("Transaction 1", transactions[0].Subject);
        Assert.Equal("Transaction 2", transactions[1].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_Pagination_SecondPage_ReturnsCorrectTransactions()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await CreateTransactionAsync(TransactionType.EXPENSE, i * 10m, new DateOnly(2024, 1, i), $"Transaction {i}");
        }

        var filter = CreateFilter(page: 2, pageSize: 2, sortDescending: false);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count);
        Assert.Equal("Transaction 3", transactions[0].Subject);
        Assert.Equal("Transaction 4", transactions[1].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_Pagination_LastPage_ReturnsRemainingTransactions()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await CreateTransactionAsync(TransactionType.EXPENSE, i * 10m, new DateOnly(2024, 1, i), $"Transaction {i}");
        }

        var filter = CreateFilter(page: 3, pageSize: 2, sortDescending: false);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Equal("Transaction 5", transactions[0].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_Pagination_BeyondLastPage_ReturnsEmpty()
    {
        // Arrange
        for (int i = 1; i <= 3; i++)
        {
            await CreateTransactionAsync(TransactionType.EXPENSE, i * 10m, new DateOnly(2024, 1, i), $"Transaction {i}");
        }

        var filter = CreateFilter(page: 5, pageSize: 2);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_LargePageSize_ReturnsAllTransactions()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await CreateTransactionAsync(TransactionType.EXPENSE, i * 10m, new DateOnly(2024, 1, i), $"Transaction {i}");
        }

        var filter = CreateFilter(page: 1, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(10, transactions.Count);
    }

    #endregion

    #region Combined Filters Tests

    [Fact]
    public async Task GetByUserIdWithFilterAsync_CombineTypeAndAmountFilters_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 25m, new DateOnly(2024, 1, 1), "Small expense");
        await CreateTransactionAsync(TransactionType.EXPENSE, 75m, new DateOnly(2024, 1, 2), "Large expense");
        await CreateTransactionAsync(TransactionType.INCOME, 100m, new DateOnly(2024, 1, 3), "Small income");
        await CreateTransactionAsync(TransactionType.INCOME, 200m, new DateOnly(2024, 1, 4), "Large income");

        var filter = CreateFilter(
            transactionType: TransactionType.EXPENSE,
            minAmount: 50m,
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Equal(TransactionType.EXPENSE, transactions[0].TransactionType);
        Assert.True(transactions[0].Amount >= 50m);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_CombineDateAndPaymentMethodFilters_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Before range", PaymentMethod.CASH);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "In range cash", PaymentMethod.CASH);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 6), "In range card", PaymentMethod.DEBIT_CARD);
        await CreateTransactionAsync(TransactionType.EXPENSE, 80m, new DateOnly(2024, 1, 15), "After range", PaymentMethod.CASH);

        var filter = CreateFilter(
            dateFrom: new DateOnly(2024, 1, 5),
            dateTo: new DateOnly(2024, 1, 10),
            paymentMethods: new[] { PaymentMethod.CASH },
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Equal("In range cash", transactions[0].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_CombineCategoryAndGroupFilters_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Cat1 Group1",
            categoryId: _testCategory1.Id, transactionGroupId: _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Cat2 Group1",
            categoryId: _testCategory2.Id, transactionGroupId: _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Cat1 Group2",
            categoryId: _testCategory1.Id, transactionGroupId: _testGroup2.Id);

        var filter = CreateFilter(
            categoryIds: new[] { _testCategory1.Id },
            transactionGroupIds: new[] { _testGroup1.Id },
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Equal("Cat1 Group1", transactions[0].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_CombineTextSearchFilters_ReturnsMatchingTransactions()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Coffee shop", notes: "urgent payment");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 2), "Coffee house", notes: "regular");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 3), "Tea shop", notes: "urgent payment");

        var filter = CreateFilter(
            subjectContains: "coffee",
            notesContains: "urgent",
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Equal("Coffee shop", transactions[0].Subject);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_ComplexFilterCombination_ReturnsMatchingTransactions()
    {
        // Arrange - create various transactions
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 5), "Coffee expense",
            PaymentMethod.CASH, "urgent", _testCategory1.Id, _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 75m, new DateOnly(2024, 1, 6), "Coffee shop",
            PaymentMethod.DEBIT_CARD, "urgent", _testCategory1.Id, _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.INCOME, 100m, new DateOnly(2024, 1, 7), "Coffee sale",
            PaymentMethod.CASH, "urgent", _testCategory1.Id, _testGroup1.Id);
        await CreateTransactionAsync(TransactionType.EXPENSE, 150m, new DateOnly(2024, 1, 8), "Coffee wholesale",
            PaymentMethod.CASH, "urgent", _testCategory1.Id, _testGroup1.Id);

        var filter = CreateFilter(
            transactionType: TransactionType.EXPENSE,
            minAmount: 50m,
            maxAmount: 100m,
            dateFrom: new DateOnly(2024, 1, 5),
            dateTo: new DateOnly(2024, 1, 10),
            subjectContains: "coffee",
            notesContains: "urgent",
            paymentMethods: new[] { PaymentMethod.CASH, PaymentMethod.DEBIT_CARD },
            categoryIds: new[] { _testCategory1.Id },
            transactionGroupIds: new[] { _testGroup1.Id },
            pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(2, transactions.Count); // Should return first two transactions
        Assert.All(transactions, t =>
        {
            Assert.Equal(TransactionType.EXPENSE, t.TransactionType);
            Assert.True(t.Amount >= 50m && t.Amount <= 100m);
            Assert.Contains("coffee", t.Subject, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("urgent", t.Notes ?? "", StringComparison.OrdinalIgnoreCase);
        });
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task GetByUserIdWithFilterAsync_OnlyReturnsTransactionsForSpecifiedUser()
    {
        // Arrange - create another user
        var userRepo = new UserRepository(_fixture.DbContext);
        var otherUserResult = await userRepo.CreateAsync(new User(
            name: "Other User",
            email: $"other-{Guid.NewGuid():N}@test.com",
            passwordHash: "hashed_password",
            initialBalance: 0
        ), CancellationToken.None);
        var otherUser = otherUserResult.Value;

        // Create transaction for test user
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Test user transaction");

        // Create transaction for other user
        var otherUserTransaction = new Transaction(
            userId: otherUser.Id,
            transactionType: TransactionType.EXPENSE,
            amount: 100m,
            date: new DateOnly(2024, 1, 1),
            subject: "Other user transaction",
            paymentMethod: PaymentMethod.CASH,
            notes: null,
            categoryId: null,
            transactionGroupId: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
        await _repository.CreateAsync(otherUserTransaction, CancellationToken.None);

        var filter = CreateFilter(pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Single(transactions);
        Assert.Equal(_testUser.Id, transactions[0].UserId);
        Assert.Equal("Test user transaction", transactions[0].Subject);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetByUserIdWithFilterAsync_NoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var filter = CreateFilter(pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_NoMatchingTransactions_ReturnsEmptyList()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Expense");

        var filter = CreateFilter(transactionType: TransactionType.INCOME, pageSize: 100);

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_DefaultFilter_ReturnsAllTransactionsSortedByDateDescending()
    {
        // Arrange
        await CreateTransactionAsync(TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Oldest");
        await CreateTransactionAsync(TransactionType.EXPENSE, 60m, new DateOnly(2024, 1, 5), "Middle");
        await CreateTransactionAsync(TransactionType.EXPENSE, 70m, new DateOnly(2024, 1, 10), "Newest");

        var filter = new TransactionFilter(); // All defaults

        // Act
        var result = await _repository.GetByUserIdWithFilterAsync(_testUser.Id, filter, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var transactions = result.Value;
        Assert.Equal(3, transactions.Count);
        // Default sort is by Date descending
        Assert.Equal("Newest", transactions[0].Subject);
        Assert.Equal("Middle", transactions[1].Subject);
        Assert.Equal("Oldest", transactions[2].Subject);
    }

    #endregion
}
