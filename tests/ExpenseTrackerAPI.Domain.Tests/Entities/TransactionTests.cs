using ExpenseTrackerAPI.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTrackerAPI.Domain.Tests.Entities;

/// <summary>
/// Tests for Transaction entity constructor validation and business rules
/// </summary>
public class TransactionTests
{
    #region Test Data Helpers

    private static readonly DateOnly ValidDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
    private const int ValidUserId = 1;
    private const decimal ValidAmount = 100.50m;
    private const string ValidSubject = "Test transaction";
    private const PaymentMethod ValidPaymentMethod = PaymentMethod.CREDIT_CARD;
    private const int ValidCategoryId = 1;
    private const int ValidTransactionGroupId = 1;

    #endregion

    #region Valid Construction Tests

    [Fact]
    public void Constructor_WithValidExpenseData_ShouldCreateTransaction()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod,
            "Some notes",
            ValidCategoryId,
            ValidTransactionGroupId);

        // Assert
        transaction.Should().NotBeNull();
        transaction.UserId.Should().Be(ValidUserId);
        transaction.TransactionType.Should().Be(TransactionType.EXPENSE);
        transaction.Amount.Should().Be(ValidAmount);
        transaction.SignedAmount.Should().Be(-ValidAmount); // Negative for expense
        transaction.Date.Should().Be(ValidDate);
        transaction.Subject.Should().Be(ValidSubject);
        transaction.Notes.Should().Be("Some notes");
        transaction.PaymentMethod.Should().Be(ValidPaymentMethod);
        transaction.CategoryId.Should().Be(ValidCategoryId);
        transaction.TransactionGroupId.Should().Be(ValidTransactionGroupId);
        transaction.CumulativeDelta.Should().Be(0); // Set by service layer
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        transaction.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithValidIncomeData_ShouldCreateTransaction()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.INCOME,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod,
            null,
            ValidCategoryId,
            null);

        // Assert
        transaction.Should().NotBeNull();
        transaction.TransactionType.Should().Be(TransactionType.INCOME);
        transaction.SignedAmount.Should().Be(ValidAmount); // Positive for income
        transaction.CategoryId.Should().Be(ValidCategoryId);
        transaction.TransactionGroupId.Should().BeNull();
        transaction.Notes.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithoutOptionalParameters_ShouldCreateTransaction()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Notes.Should().BeNull();
        transaction.CategoryId.Should().BeNull();
        transaction.TransactionGroupId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithWhitespaceNotes_ShouldTrimAndSetToNull()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod,
            "   ");

        // Assert
        transaction.Notes.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithWhitespaceSubject_ShouldTrim()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            "  Valid Subject  ",
            ValidPaymentMethod);

        // Assert
        transaction.Subject.Should().Be("Valid Subject");
    }

    #endregion

    #region User ID Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidUserId_ShouldThrowArgumentException(int invalidUserId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                invalidUserId,
                TransactionType.EXPENSE,
                ValidAmount,
                ValidDate,
                ValidSubject,
                ValidPaymentMethod));

        exception.Message.Should().Contain("User ID must be a positive integer");
        exception.ParamName.Should().Be("userId");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public void Constructor_WithValidUserId_ShouldSucceed(int validUserId)
    {
        // Act
        var transaction = new Transaction(
            validUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.UserId.Should().Be(validUserId);
    }

    #endregion

    #region Amount Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Constructor_WithInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                ValidUserId,
                TransactionType.EXPENSE,
                invalidAmount,
                ValidDate,
                ValidSubject,
                ValidPaymentMethod));

        exception.Message.Should().Contain("Transaction amount must be greater than zero");
        exception.ParamName.Should().Be("amount");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(999999.99)]
    public void Constructor_WithValidAmount_ShouldSucceed(decimal validAmount)
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            validAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Amount.Should().Be(validAmount);
    }

    [Fact]
    public void Constructor_WithLargeAmount_ShouldSucceed()
    {
        // Arrange
        var largeAmount = 1_000_000m;

        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            largeAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Amount.Should().Be(largeAmount);
    }

    #endregion

    #region Date Validation Tests

    [Fact]
    public void Constructor_WithDateTooOld_ShouldThrowArgumentException()
    {
        // Arrange
        var tooOldDate = new DateOnly(1899, 12, 31);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                ValidUserId,
                TransactionType.EXPENSE,
                ValidAmount,
                tooOldDate,
                ValidSubject,
                ValidPaymentMethod));

        exception.Message.Should().Contain("Transaction date must be between");
        exception.ParamName.Should().Be("date");
    }

    [Fact]
    public void Constructor_WithFutureDate_ShouldThrowArgumentException()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2));

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                ValidUserId,
                TransactionType.EXPENSE,
                ValidAmount,
                futureDate,
                ValidSubject,
                ValidPaymentMethod));

        exception.Message.Should().Contain("Transaction date must be between");
        exception.ParamName.Should().Be("date");
    }

    [Fact]
    public void Constructor_WithDateAtMinBoundary_ShouldSucceed()
    {
        // Arrange
        var minDate = new DateOnly(1900, 1, 1);

        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            minDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Date.Should().Be(minDate);
    }

    [Fact]
    public void Constructor_WithDateAtMaxBoundary_ShouldSucceed()
    {
        // Arrange
        var maxDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1));

        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            maxDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Date.Should().Be(maxDate);
    }

    [Fact]
    public void Constructor_WithToday_ShouldSucceed()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            today,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Date.Should().Be(today);
    }

    #endregion

    #region Subject Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithInvalidSubject_ShouldThrowArgumentException(string invalidSubject)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                ValidUserId,
                TransactionType.EXPENSE,
                ValidAmount,
                ValidDate,
                invalidSubject,
                ValidPaymentMethod));

        exception.Message.Should().Contain("Transaction subject is required and cannot be empty");
        exception.ParamName.Should().Be("subject");
    }

    [Fact]
    public void Constructor_WithSubjectTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longSubject = new string('A', 256); // 256 characters

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                ValidUserId,
                TransactionType.EXPENSE,
                ValidAmount,
                ValidDate,
                longSubject,
                ValidPaymentMethod));

        exception.Message.Should().Contain("Transaction subject cannot exceed 255 characters");
        exception.ParamName.Should().Be("subject");
    }

    [Fact]
    public void Constructor_WithSubjectAtMaxLength_ShouldSucceed()
    {
        // Arrange
        var maxLengthSubject = new string('A', 255); // 255 characters

        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            maxLengthSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Subject.Should().Be(maxLengthSubject);
    }

    [Theory]
    [InlineData("Valid subject")]
    [InlineData("A")]
    [InlineData("Grocery shopping at Whole Foods")]
    public void Constructor_WithValidSubject_ShouldSucceed(string validSubject)
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            validSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Subject.Should().Be(validSubject);
    }

    #endregion

    #region Category Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidCategoryId_ShouldThrowArgumentException(int invalidCategoryId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new Transaction(
                ValidUserId,
                TransactionType.EXPENSE,
                ValidAmount,
                ValidDate,
                ValidSubject,
                ValidPaymentMethod,
                null,
                invalidCategoryId));

        exception.Message.Should().Contain("Category ID must be a positive integer when provided");
        exception.ParamName.Should().Be("categoryId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(999)]
    public void Constructor_WithValidCategoryId_ShouldSucceed(int? validCategoryId)
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod,
            null,
            validCategoryId);

        // Assert
        transaction.CategoryId.Should().Be(validCategoryId);
    }

    #endregion

    #region Transaction Type Tests

    [Fact]
    public void Constructor_WithExpenseType_ShouldSetNegativeSignedAmount()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.TransactionType.Should().Be(TransactionType.EXPENSE);
        transaction.SignedAmount.Should().Be(-ValidAmount);
    }

    [Fact]
    public void Constructor_WithIncomeType_ShouldSetPositiveSignedAmount()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.INCOME,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Assert
        transaction.TransactionType.Should().Be(TransactionType.INCOME);
        transaction.SignedAmount.Should().Be(ValidAmount);
    }

    #endregion

    #region UpdateCumulativeDelta Tests

    [Fact]
    public void UpdateCumulativeDelta_ShouldUpdateValueAndTimestamp()
    {
        // Arrange
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        var originalUpdatedAt = transaction.UpdatedAt;
        var newCumulativeDelta = 1500.75m;

        // Wait a bit to ensure timestamp changes
        Thread.Sleep(10);

        // Act
        transaction.UpdateCumulativeDelta(newCumulativeDelta);

        // Assert
        transaction.CumulativeDelta.Should().Be(newCumulativeDelta);
        transaction.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000.50)]
    [InlineData(1000.50)]
    [InlineData(999999.99)]
    public void UpdateCumulativeDelta_WithVariousValues_ShouldWork(decimal cumulativeDelta)
    {
        // Arrange
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        // Act
        transaction.UpdateCumulativeDelta(cumulativeDelta);

        // Assert
        transaction.CumulativeDelta.Should().Be(cumulativeDelta);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Constructor_WithAllOptionalParametersNull_ShouldSucceed()
    {
        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.INCOME,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod,
            null,
            null,
            null);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Notes.Should().BeNull();
        transaction.CategoryId.Should().BeNull();
        transaction.TransactionGroupId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMinimalValidData_ShouldCreateValidTransaction()
    {
        // Arrange
        var minimalSubject = "A";
        var minimalAmount = 0.01m;
        var minimalUserId = 1;

        // Act
        var transaction = new Transaction(
            minimalUserId,
            TransactionType.EXPENSE,
            minimalAmount,
            ValidDate,
            minimalSubject,
            ValidPaymentMethod);

        // Assert
        transaction.Should().NotBeNull();
        transaction.UserId.Should().Be(minimalUserId);
        transaction.Amount.Should().Be(minimalAmount);
        transaction.Subject.Should().Be(minimalSubject);
    }

    [Fact]
    public void Constructor_ShouldSetTimestampsToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var transaction = new Transaction(
            ValidUserId,
            TransactionType.EXPENSE,
            ValidAmount,
            ValidDate,
            ValidSubject,
            ValidPaymentMethod);

        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        transaction.CreatedAt.Should().BeAfter(beforeCreation);
        transaction.CreatedAt.Should().BeBefore(afterCreation);
        transaction.UpdatedAt.Should().BeAfter(beforeCreation);
        transaction.UpdatedAt.Should().BeBefore(afterCreation);
        transaction.CreatedAt.Should().BeCloseTo(transaction.UpdatedAt, TimeSpan.FromMilliseconds(100));
    }

    #endregion
}
