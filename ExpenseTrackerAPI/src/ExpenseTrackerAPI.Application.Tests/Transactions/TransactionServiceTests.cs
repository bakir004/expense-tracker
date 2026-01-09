using ErrorOr;
using Moq;
using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Tests.Transactions;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<ITransactionGroupRepository> _mockTransactionGroupRepository;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockTransactionGroupRepository = new Mock<ITransactionGroupRepository>();
        _service = new TransactionService(
            _mockTransactionRepository.Object,
            _mockUserRepository.Object,
            _mockCategoryRepository.Object,
            _mockTransactionGroupRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnTransactions_WhenRepositoryReturnsTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction { Id = 1, UserId = 1, TransactionType = TransactionType.Expense, Amount = 50m, SignedAmount = -50m },
            new Transaction { Id = 2, UserId = 1, TransactionType = TransactionType.Income, Amount = 100m, SignedAmount = 100m }
        };
        _mockTransactionRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Transactions.Count);
        Assert.Equal(100m, result.Value.TotalIncome);
        Assert.Equal(50m, result.Value.TotalExpenses);
        Assert.Equal(50m, result.Value.NetChange);
        Assert.Equal(1, result.Value.IncomeCount);
        Assert.Equal(1, result.Value.ExpenseCount);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnError_WhenRepositoryReturnsError()
    {
        // Arrange
        var error = Error.Failure("database", "Database error");
        _mockTransactionRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(error);

        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(error, result.Errors);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction_WhenTransactionExists()
    {
        // Arrange
        var transaction = new Transaction { Id = 1, UserId = 1, TransactionType = TransactionType.Expense, Amount = 50m };
        _mockTransactionRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(transaction.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenTransactionDoesNotExist()
    {
        // Arrange
        _mockTransactionRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionErrors.NotFound);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnTransactions_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var transactions = new List<Transaction>
        {
            new Transaction { Id = 1, UserId = userId, TransactionType = TransactionType.Expense, Amount = 50m, SignedAmount = -50m }
        };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTransactionRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(transactions, result.Value.Transactions);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _service.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.NotFound, result.Errors);
        _mockTransactionRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByUserIdAndTypeAsync_ShouldReturnTransactions_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var type = TransactionType.Expense;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var transactions = new List<Transaction>
        {
            new Transaction { Id = 1, UserId = userId, TransactionType = type, Amount = 50m, SignedAmount = -50m }
        };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTransactionRepository
            .Setup(r => r.GetByUserIdAndTypeAsync(userId, type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.GetByUserIdAndTypeAsync(userId, type, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.All(result.Value.Transactions, t => Assert.Equal(type, t.TransactionType));
    }

    [Fact]
    public async Task GetByUserIdAndTypeAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 999;
        var type = TransactionType.Expense;
        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _service.GetByUserIdAndTypeAsync(userId, type, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTransaction_WhenValidExpense()
    {
        // Arrange
        var userId = 1;
        var categoryId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var category = new Category { Id = categoryId, Name = "Food" };
        var transaction = new Transaction
        {
            Id = 1,
            UserId = userId,
            TransactionType = TransactionType.Expense,
            Amount = 50m,
            SignedAmount = -50m,
            Date = DateTime.UtcNow,
            Subject = "Lunch",
            CategoryId = categoryId
        };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _mockTransactionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            categoryId,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Expense, result.Value.TransactionType);
        Assert.Equal(-50m, result.Value.SignedAmount);
        Assert.Equal(categoryId, result.Value.CategoryId);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTransaction_WhenValidIncome()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var transaction = new Transaction
        {
            Id = 1,
            UserId = userId,
            TransactionType = TransactionType.Income,
            Amount = 100m,
            SignedAmount = 100m,
            Date = DateTime.UtcNow,
            Subject = "Salary",
            IncomeSource = "Work"
        };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTransactionRepository
            .Setup(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Income,
            100m,
            DateTime.UtcNow,
            "Salary",
            null,
            PaymentMethod.BankTransfer,
            null,
            null,
            "Work",
            CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(TransactionType.Income, result.Value.TransactionType);
        Assert.Equal(100m, result.Value.SignedAmount);
        Assert.Equal("Work", result.Value.IncomeSource);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenSubjectIsEmpty()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "",
            null,
            PaymentMethod.Cash,
            1,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionErrors.InvalidSubject, result.Errors);
        _mockTransactionRepository.Verify(r => r.CreateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenAmountIsZero()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            0m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            1,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionErrors.InvalidAmount, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenExpenseMissingCategory()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            null, // Missing category
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionErrors.ExpenseMissingCategory, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            1,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(UserErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCategoryNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var categoryId = 999;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CategoryErrors.NotFound);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            categoryId,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(CategoryErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTransactionGroupNotFound_WhenTransactionGroupDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var categoryId = 1;
        var transactionGroupId = 999;
        var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
        var category = new Category { Id = categoryId, Name = "Food" };

        _mockUserRepository
            .Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _mockTransactionGroupRepository
            .Setup(r => r.GetByIdAsync(transactionGroupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionGroupErrors.NotFound);

        // Act
        var result = await _service.CreateAsync(
            userId,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            categoryId,
            transactionGroupId,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionGroupErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTransaction_WhenValid()
    {
        // Arrange
        var id = 1;
        var userId = 1;
        var categoryId = 1;
        var existingTransaction = new Transaction
        {
            Id = id,
            UserId = userId,
            TransactionType = TransactionType.Expense,
            Amount = 50m,
            SignedAmount = -50m
        };
        var category = new Category { Id = categoryId, Name = "Food" };
        var updatedTransaction = new Transaction
        {
            Id = id,
            UserId = userId,
            TransactionType = TransactionType.Expense,
            Amount = 75m,
            SignedAmount = -75m,
            CategoryId = categoryId
        };

        _mockTransactionRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);

        _mockCategoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _mockTransactionRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTransaction);

        // Act
        var result = await _service.UpdateAsync(
            id,
            TransactionType.Expense,
            75m,
            DateTime.UtcNow,
            "Dinner",
            null,
            PaymentMethod.Cash,
            categoryId,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(75m, result.Value.Amount);
        Assert.Equal(-75m, result.Value.SignedAmount);
        Assert.Equal(userId, result.Value.UserId); // UserId should be preserved
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenTransactionDoesNotExist()
    {
        // Arrange
        var id = 999;
        _mockTransactionRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionErrors.NotFound);

        // Act
        var result = await _service.UpdateAsync(
            id,
            TransactionType.Expense,
            50m,
            DateTime.UtcNow,
            "Lunch",
            null,
            PaymentMethod.Cash,
            1,
            null,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionErrors.NotFound, result.Errors);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnDeleted_WhenTransactionExists()
    {
        // Arrange
        var id = 1;
        _mockTransactionRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Deleted);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _mockTransactionRepository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnError_WhenTransactionDoesNotExist()
    {
        // Arrange
        var id = 999;
        _mockTransactionRepository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionErrors.NotFound);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(TransactionErrors.NotFound, result.Errors);
    }
}

