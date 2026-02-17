using ErrorOr;
using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using FluentAssertions;
using Moq;
using Xunit;

namespace ExpenseTrackerAPI.Application.Tests.Transactions;

/// <summary>
/// Application layer tests for TransactionService filtering functionality.
/// Tests the GetByUserIdWithFilterAsync method which coordinates between repositories
/// and transforms domain entities to response DTOs.
/// </summary>
public class TransactionServiceFilteringTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly TransactionService _sut;

    public TransactionServiceFilteringTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _sut = new TransactionService(_transactionRepositoryMock.Object, _userRepositoryMock.Object);
    }

    #region Helper Methods

    private static User CreateTestUser(int id = 1)
    {
        return new User(
            name: "Test User",
            email: "test@example.com",
            passwordHash: "hashed_password",
            initialBalance: 1000m
        );
    }

    private static Transaction CreateTestTransaction(
        int id,
        int userId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string subject)
    {
        var transaction = new Transaction(
            userId: userId,
            transactionType: type,
            amount: amount,
            date: date,
            subject: subject,
            paymentMethod: PaymentMethod.CASH,
            notes: null,
            categoryId: null,
            transactionGroupId: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
        transaction.UpdateId(id);
        return transaction;
    }

    #endregion

    [Fact]
    public async Task GetByUserIdWithFilterAsync_UserExists_ReturnsFilteredTransactionsAsResponse()
    {
        // Arrange
        const int userId = 1;
        var user = CreateTestUser(userId);
        var filter = new TransactionFilter
        {
            TransactionType = TransactionType.EXPENSE,
            Page = 1,
            PageSize = 20
        };

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(1, userId, TransactionType.EXPENSE, 50m, new DateOnly(2024, 1, 1), "Expense 1"),
            CreateTestTransaction(2, userId, TransactionType.EXPENSE, 75m, new DateOnly(2024, 1, 2), "Expense 2")
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionRepositoryMock
            .Setup(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync((transactions, transactions.Count));

        // Act
        var result = await _sut.GetByUserIdWithFilterAsync(userId, filter, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Transactions.Should().HaveCount(2);
        result.Value.Transactions[0].Subject.Should().Be("Expense 1");
        result.Value.Transactions[1].Subject.Should().Be("Expense 2");

        // Verify repository interactions
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepositoryMock.Verify(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_UserDoesNotExist_ReturnsUserNotFoundError()
    {
        // Arrange
        const int userId = 999;
        var filter = new TransactionFilter
        {
            Page = 1,
            PageSize = 20
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _sut.GetByUserIdWithFilterAsync(userId, filter, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.NotFound);

        // Verify user repository was called but transaction repository was not
        _userRepositoryMock.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepositoryMock.Verify(x => x.GetByUserIdWithFilterAsync(It.IsAny<int>(), It.IsAny<TransactionFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_RepositoryReturnsError_PropagatesError()
    {
        // Arrange
        const int userId = 1;
        var user = CreateTestUser(userId);
        var filter = new TransactionFilter { Page = 1, PageSize = 20 };

        var databaseError = Error.Failure("Database.Error", "Failed to retrieve transactions");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionRepositoryMock
            .Setup(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(databaseError);

        // Act
        var result = await _sut.GetByUserIdWithFilterAsync(userId, filter, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(databaseError);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_NoMatchingTransactions_ReturnsEmptyList()
    {
        // Arrange
        const int userId = 1;
        var user = CreateTestUser(userId);
        var filter = new TransactionFilter
        {
            TransactionType = TransactionType.INCOME,
            Page = 1,
            PageSize = 20
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionRepositoryMock
            .Setup(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>(), 0));

        // Act
        var result = await _sut.GetByUserIdWithFilterAsync(userId, filter, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_ComplexFilter_PassesFilterToRepository()
    {
        // Arrange
        const int userId = 1;
        var user = CreateTestUser(userId);
        var filter = new TransactionFilter
        {
            TransactionType = TransactionType.EXPENSE,
            MinAmount = 50m,
            MaxAmount = 200m,
            DateFrom = new DateOnly(2024, 1, 1),
            DateTo = new DateOnly(2024, 12, 31),
            SubjectContains = "coffee",
            PaymentMethods = new List<PaymentMethod> { PaymentMethod.CASH, PaymentMethod.DEBIT_CARD },
            CategoryIds = new List<int> { 1, 2 },
            SortBy = TransactionSortField.Amount,
            SortDescending = true,
            Page = 2,
            PageSize = 10
        };

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(1, userId, TransactionType.EXPENSE, 100m, new DateOnly(2024, 6, 15), "Coffee shop")
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionRepositoryMock
            .Setup(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync((transactions, transactions.Count));

        // Act
        var result = await _sut.GetByUserIdWithFilterAsync(userId, filter, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Transactions.Should().HaveCount(1);

        // Verify the exact filter was passed to repository
        _transactionRepositoryMock.Verify(x => x.GetByUserIdWithFilterAsync(
            It.Is<int>(id => id == userId),
            It.Is<TransactionFilter>(f =>
                f.TransactionType == TransactionType.EXPENSE &&
                f.MinAmount == 50m &&
                f.MaxAmount == 200m &&
                f.DateFrom == new DateOnly(2024, 1, 1) &&
                f.DateTo == new DateOnly(2024, 12, 31) &&
                f.SubjectContains == "coffee" &&
                f.SortBy == TransactionSortField.Amount &&
                f.SortDescending == true &&
                f.Page == 2 &&
                f.PageSize == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_TransactionsReturned_MapsAllFieldsToResponse()
    {
        // Arrange
        const int userId = 1;
        var user = CreateTestUser(userId);
        var filter = new TransactionFilter { Page = 1, PageSize = 20 };

        var transaction = new Transaction(
            userId: userId,
            transactionType: TransactionType.EXPENSE,
            amount: 150.50m,
            date: new DateOnly(2024, 3, 15),
            subject: "Grocery Shopping",
            paymentMethod: PaymentMethod.CREDIT_CARD,
            notes: "Weekly groceries",
            categoryId: 5,
            transactionGroupId: 10,
            createdAt: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            updatedAt: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc)
        );
        transaction.UpdateId(42);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionRepositoryMock
            .Setup(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction> { transaction }, 1));

        // Act
        var result = await _sut.GetByUserIdWithFilterAsync(userId, filter, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        var responseTransaction = result.Value.Transactions.Should().ContainSingle().Subject;

        responseTransaction.Id.Should().Be(42);
        responseTransaction.UserId.Should().Be(userId);
        responseTransaction.TransactionType.Should().Be("EXPENSE");
        responseTransaction.Amount.Should().Be(150.50m);
        responseTransaction.Date.Should().Be(new DateOnly(2024, 3, 15));
        responseTransaction.Subject.Should().Be("Grocery Shopping");
        responseTransaction.PaymentMethod.Should().Be("CREDIT_CARD");
        responseTransaction.Notes.Should().Be("Weekly groceries");
        responseTransaction.CategoryId.Should().Be(5);
        responseTransaction.TransactionGroupId.Should().Be(10);
    }

    [Fact]
    public async Task GetByUserIdWithFilterAsync_CancellationRequested_PropagatesCancellationToken()
    {
        // Arrange
        const int userId = 1;
        var user = CreateTestUser(userId);
        var filter = new TransactionFilter { Page = 1, PageSize = 20 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionRepositoryMock
            .Setup(x => x.GetByUserIdWithFilterAsync(userId, filter, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _sut.GetByUserIdWithFilterAsync(userId, filter, cts.Token));
    }
}
