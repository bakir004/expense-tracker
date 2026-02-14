using ErrorOr;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ExpenseTrackerAPI.Application.Tests.Transactions;

/// <summary>
/// Unit tests for TransactionFilterParser.
/// Tests validation logic for transaction filtering and query parameters.
/// </summary>
public class TransactionFilterParserTests
{
    #region Null and Default Tests

    [Fact]
    public void Parse_WithNullRequest_ShouldReturnDefaultFilter()
    {
        // Act
        var result = TransactionFilterParser.Parse(null);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.TransactionType.Should().BeNull();
        result.Value.MinAmount.Should().BeNull();
        result.Value.MaxAmount.Should().BeNull();
        result.Value.DateFrom.Should().BeNull();
        result.Value.DateTo.Should().BeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public void Parse_WithEmptyRequest_ShouldReturnDefaultFilter()
    {
        // Arrange
        var request = new TransactionFilterRequest();

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TransactionType.Should().BeNull();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    #endregion

    #region Transaction Type Tests

    [Theory]
    [InlineData("EXPENSE", TransactionType.EXPENSE)]
    [InlineData("expense", TransactionType.EXPENSE)]
    [InlineData("Expense", TransactionType.EXPENSE)]
    [InlineData("INCOME", TransactionType.INCOME)]
    [InlineData("income", TransactionType.INCOME)]
    [InlineData("Income", TransactionType.INCOME)]
    public void Parse_WithValidTransactionType_ShouldParseCorrectly(string input, TransactionType expected)
    {
        // Arrange
        var request = new TransactionFilterRequest { TransactionType = input };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TransactionType.Should().Be(expected);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("transfer")]
    [InlineData("123")]
    public void Parse_WithInvalidTransactionType_ShouldReturnValidationError(string input)
    {
        // Arrange
        var request = new TransactionFilterRequest { TransactionType = input };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Type.Should().Be(ErrorType.Validation);
        result.Errors[0].Code.Should().Be("TransactionType");
        result.Errors[0].Code.Should().NotContain(".");
    }

    [Fact]
    public void Parse_WithNullTransactionType_ShouldNotSetTransactionType()
    {
        // Arrange
        var request = new TransactionFilterRequest { TransactionType = null };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TransactionType.Should().BeNull();
    }

    #endregion

    #region Amount Range Tests

    [Theory]
    [InlineData(0, 100)]
    [InlineData(10.50, 500.75)]
    [InlineData(0, 0)]
    [InlineData(100, 100)]
    public void Parse_WithValidAmountRange_ShouldParseCorrectly(decimal min, decimal max)
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            MinAmount = min,
            MaxAmount = max
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.MinAmount.Should().Be(min);
        result.Value.MaxAmount.Should().Be(max);
    }

    [Fact]
    public void Parse_WithNegativeMinAmount_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { MinAmount = -10.50m };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("MinAmount");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("negative");
    }

    [Fact]
    public void Parse_WithNegativeMaxAmount_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { MaxAmount = -50m };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("MaxAmount");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("negative");
    }

    [Fact]
    public void Parse_WithMinAmountGreaterThanMaxAmount_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            MinAmount = 100m,
            MaxAmount = 50m
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("AmountRange");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("greater than");
    }

    [Fact]
    public void Parse_WithOnlyMinAmount_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { MinAmount = 50m };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.MinAmount.Should().Be(50m);
        result.Value.MaxAmount.Should().BeNull();
    }

    [Fact]
    public void Parse_WithOnlyMaxAmount_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { MaxAmount = 200m };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.MinAmount.Should().BeNull();
        result.Value.MaxAmount.Should().Be(200m);
    }

    #endregion

    #region Date Range Tests

    [Theory]
    [InlineData("2024-01-01", "2024-12-31")]
    [InlineData("2024-06-15", "2024-06-15")]
    [InlineData("2020-01-01", "2025-12-31")]
    public void Parse_WithValidDateRange_ShouldParseCorrectly(string from, string to)
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            DateFrom = from,
            DateTo = to
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DateFrom.Should().Be(DateOnly.Parse(from));
        result.Value.DateTo.Should().Be(DateOnly.Parse(to));
    }

    [Theory]
    [InlineData("invalid-date")]
    [InlineData("2024/01/01")]
    [InlineData("01-01-2024")]
    [InlineData("2024-13-01")]
    [InlineData("2024-01-32")]
    [InlineData("abc")]
    public void Parse_WithInvalidDateFromFormat_ShouldReturnValidationError(string invalidDate)
    {
        // Arrange
        var request = new TransactionFilterRequest { DateFrom = invalidDate };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("DateFrom");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("Invalid date format");
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("12/31/2024")]
    [InlineData("2024.12.31")]
    public void Parse_WithInvalidDateToFormat_ShouldReturnValidationError(string invalidDate)
    {
        // Arrange
        var request = new TransactionFilterRequest { DateTo = invalidDate };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("DateTo");
        result.Errors[0].Code.Should().NotContain(".");
    }

    [Fact]
    public void Parse_WithDateFromAfterDateTo_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            DateFrom = "2024-12-31",
            DateTo = "2024-01-01"
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("DateRange");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("cannot be after");
    }

    [Fact]
    public void Parse_WithOnlyDateFrom_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { DateFrom = "2024-01-01" };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DateFrom.Should().Be(new DateOnly(2024, 1, 1));
        result.Value.DateTo.Should().BeNull();
    }

    [Fact]
    public void Parse_WithOnlyDateTo_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { DateTo = "2024-12-31" };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DateFrom.Should().BeNull();
        result.Value.DateTo.Should().Be(new DateOnly(2024, 12, 31));
    }

    #endregion

    #region Payment Method Tests

    [Fact]
    public void Parse_WithValidPaymentMethods_Cash_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { PaymentMethods = new[] { "CASH" } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PaymentMethods.Should().NotBeNull();
        result.Value.PaymentMethods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Parse_WithValidPaymentMethods_CaseInsensitive_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { PaymentMethods = new[] { "cash" } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PaymentMethods.Should().NotBeNull();
        result.Value.PaymentMethods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Parse_WithValidPaymentMethods_BankTransfer_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { PaymentMethods = new[] { "BANK_TRANSFER" } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PaymentMethods.Should().NotBeNull();
        result.Value.PaymentMethods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Parse_WithValidPaymentMethods_Multiple_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { PaymentMethods = new[] { "CREDIT_CARD", "CASH" } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PaymentMethods.Should().NotBeNull();
        result.Value.PaymentMethods.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_WithInvalidPaymentMethod_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            PaymentMethods = new[] { "INVALID_METHOD", "CASH" }
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("PaymentMethods");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("Invalid payment method");
    }

    [Fact]
    public void Parse_WithNullPaymentMethods_ShouldNotSetPaymentMethods()
    {
        // Arrange
        var request = new TransactionFilterRequest { PaymentMethods = null };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PaymentMethods.Should().BeNull();
    }

    [Fact]
    public void Parse_WithEmptyPaymentMethodsArray_ShouldNotSetPaymentMethods()
    {
        // Arrange
        var request = new TransactionFilterRequest { PaymentMethods = Array.Empty<string>() };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PaymentMethods.Should().BeNull();
    }

    #endregion

    #region Pagination Tests

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 20)]
    [InlineData(5, 50)]
    [InlineData(100, 100)]
    public void Parse_WithValidPagination_ShouldParseCorrectly(int page, int pageSize)
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            Page = page,
            PageSize = pageSize
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Page.Should().Be(page);
        result.Value.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Parse_WithInvalidPageNumber_ShouldReturnValidationError(int page)
    {
        // Arrange
        var request = new TransactionFilterRequest { Page = page };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("Page");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("at least 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Parse_WithInvalidPageSize_ShouldReturnValidationError(int pageSize)
    {
        // Arrange
        var request = new TransactionFilterRequest { PageSize = pageSize };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("PageSize");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("at least 1");
    }

    [Fact]
    public void Parse_WithPageSizeExceedingMaximum_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { PageSize = 101 };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("PageSize");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("cannot exceed");
    }

    [Fact]
    public void Parse_WithPageSize100_ShouldBeValid()
    {
        // Arrange
        var request = new TransactionFilterRequest { PageSize = 100 };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PageSize.Should().Be(100);
    }

    [Fact]
    public void Parse_WithNullPagination_ShouldUseDefaults()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            Page = null,
            PageSize = null
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    #endregion

    #region Category and Group Tests

    [Fact]
    public void Parse_WithValidCategoryIds_Multiple_ShouldParseCorrectly()
    {
        // Arrange
        var categoryIds = new[] { 1, 2, 3 };
        var request = new TransactionFilterRequest { CategoryIds = categoryIds };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.CategoryIds.Should().NotBeNull();
        result.Value.CategoryIds.Should().BeEquivalentTo(categoryIds);
    }

    [Fact]
    public void Parse_WithValidCategoryIds_Single_ShouldParseCorrectly()
    {
        // Arrange
        var categoryIds = new[] { 100 };
        var request = new TransactionFilterRequest { CategoryIds = categoryIds };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.CategoryIds.Should().NotBeNull();
        result.Value.CategoryIds.Should().BeEquivalentTo(categoryIds);
    }

    [Fact]
    public void Parse_WithInvalidCategoryId_Zero_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { CategoryIds = new[] { 0 } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("CategoryIds");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("positive integers");
    }

    [Fact]
    public void Parse_WithInvalidCategoryId_Negative_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { CategoryIds = new[] { -1 } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("CategoryIds");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("positive integers");
    }

    [Fact]
    public void Parse_WithInvalidCategoryId_MixedValid_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { CategoryIds = new[] { 1, 2, -5 } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("CategoryIds");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("positive integers");
    }

    [Fact]
    public void Parse_WithValidTransactionGroupIds_Multiple_ShouldParseCorrectly()
    {
        // Arrange
        var groupIds = new[] { 1, 2, 3 };
        var request = new TransactionFilterRequest { TransactionGroupIds = groupIds };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TransactionGroupIds.Should().NotBeNull();
        result.Value.TransactionGroupIds.Should().BeEquivalentTo(groupIds);
    }

    [Fact]
    public void Parse_WithValidTransactionGroupIds_Single_ShouldParseCorrectly()
    {
        // Arrange
        var groupIds = new[] { 50 };
        var request = new TransactionFilterRequest { TransactionGroupIds = groupIds };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TransactionGroupIds.Should().NotBeNull();
        result.Value.TransactionGroupIds.Should().BeEquivalentTo(groupIds);
    }

    [Fact]
    public void Parse_WithInvalidTransactionGroupId_Zero_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { TransactionGroupIds = new[] { 0 } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("TransactionGroupIds");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("positive integers");
    }

    [Fact]
    public void Parse_WithInvalidTransactionGroupId_Negative_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { TransactionGroupIds = new[] { -10 } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("TransactionGroupIds");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("positive integers");
    }

    [Fact]
    public void Parse_WithInvalidTransactionGroupId_MixedValid_ShouldReturnValidationError()
    {
        // Arrange
        var request = new TransactionFilterRequest { TransactionGroupIds = new[] { 5, 10, -1 } };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("TransactionGroupIds");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("positive integers");
    }

    [Fact]
    public void Parse_WithUncategorizedFlag_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { Uncategorized = true };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Uncategorized.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithUngroupedFlag_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { Ungrouped = true };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Ungrouped.Should().BeTrue();
    }

    #endregion

    #region Sorting Tests

    [Theory]
    [InlineData("date")]
    [InlineData("Date")]
    [InlineData("DATE")]
    [InlineData("amount")]
    [InlineData("subject")]
    [InlineData("paymentMethod")]
    [InlineData("payment_method")]
    [InlineData("createdAt")]
    [InlineData("created_at")]
    public void Parse_WithValidSortBy_ShouldParseCorrectly(string sortBy)
    {
        // Arrange
        var request = new TransactionFilterRequest { SortBy = sortBy };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SortBy.Should().BeDefined();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("user")]
    [InlineData("category")]
    [InlineData("123")]
    public void Parse_WithInvalidSortBy_ShouldReturnValidationError(string sortBy)
    {
        // Arrange
        var request = new TransactionFilterRequest { SortBy = sortBy };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("SortBy");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("Invalid sort field");
    }

    [Theory]
    [InlineData("asc", false)]
    [InlineData("ASC", false)]
    [InlineData("ascending", false)]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("descending", true)]
    public void Parse_WithValidSortDirection_ShouldParseCorrectly(string direction, bool expectedDescending)
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            SortBy = "date",
            SortDirection = direction
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SortDescending.Should().Be(expectedDescending);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("123")]
    public void Parse_WithInvalidSortDirection_ShouldReturnValidationError(string direction)
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            SortBy = "date",
            SortDirection = direction
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("SortDirection");
        result.Errors[0].Code.Should().NotContain(".");
        result.Errors[0].Description.Should().Contain("Invalid sort direction");
    }

    #endregion

    #region Text Search Tests

    [Theory]
    [InlineData("grocery")]
    [InlineData("Salary payment")]
    [InlineData("Coffee at Starbucks")]
    public void Parse_WithSubjectContains_ShouldParseCorrectly(string subject)
    {
        // Arrange
        var request = new TransactionFilterRequest { SubjectContains = subject };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SubjectContains.Should().Be(subject.Trim());
    }

    [Theory]
    [InlineData("Important note")]
    [InlineData("Meeting expenses")]
    public void Parse_WithNotesContains_ShouldParseCorrectly(string notes)
    {
        // Arrange
        var request = new TransactionFilterRequest { NotesContains = notes };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.NotesContains.Should().Be(notes.Trim());
    }

    [Fact]
    public void Parse_WithWhitespaceSubject_ShouldSetToNull()
    {
        // Arrange
        var request = new TransactionFilterRequest { SubjectContains = "   " };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SubjectContains.Should().BeNull();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Parse_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            TransactionType = "INVALID",
            MinAmount = -50m,
            MaxAmount = -100m,
            Page = 0,
            PageSize = 0
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().OnlyContain(e => !e.Code.Contains("."),
            "error keys should not contain dots");
    }

    [Fact]
    public void Parse_WithAllInvalidFields_ShouldReturnMultipleErrors()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            TransactionType = "INVALID_TYPE",
            MinAmount = 200m,
            MaxAmount = 100m,
            DateFrom = "invalid-date",
            DateTo = "2024-12-31",
            Page = -1,
            PageSize = 200,
            PaymentMethods = new[] { "INVALID_METHOD" },
            CategoryIds = new[] { 0, -1 },
            SortBy = "invalid_field"
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThan(5);

        // Verify no error keys contain dots
        foreach (var error in result.Errors)
        {
            error.Code.Should().NotContain(".", $"Error code '{error.Code}' should not contain dots");
        }
    }

    #endregion

    #region Complex Filter Tests

    [Fact]
    public void Parse_WithCompleteValidFilter_ShouldParseAllFieldsCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            TransactionType = "EXPENSE",
            MinAmount = 10m,
            MaxAmount = 500m,
            DateFrom = "2024-01-01",
            DateTo = "2024-12-31",
            SubjectContains = "grocery",
            NotesContains = "weekly shopping",
            PaymentMethods = new[] { "CASH", "CREDIT_CARD" },
            CategoryIds = new[] { 1, 2, 3 },
            TransactionGroupIds = new[] { 5 },
            Uncategorized = false,
            Ungrouped = false,
            SortBy = "date",
            SortDirection = "desc",
            Page = 2,
            PageSize = 50
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.TransactionType.Should().Be(TransactionType.EXPENSE);
        result.Value.MinAmount.Should().Be(10m);
        result.Value.MaxAmount.Should().Be(500m);
        result.Value.DateFrom.Should().Be(new DateOnly(2024, 1, 1));
        result.Value.DateTo.Should().Be(new DateOnly(2024, 12, 31));
        result.Value.SubjectContains.Should().Be("grocery");
        result.Value.NotesContains.Should().Be("weekly shopping");
        result.Value.PaymentMethods.Should().HaveCount(2);
        result.Value.CategoryIds.Should().HaveCount(3);
        result.Value.TransactionGroupIds.Should().HaveCount(1);
        result.Value.Uncategorized.Should().BeFalse();
        result.Value.Ungrouped.Should().BeFalse();
        result.Value.SortDescending.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(50);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_WithVeryLargePageNumber_ShouldParseCorrectly()
    {
        // Arrange
        var request = new TransactionFilterRequest { Page = 999999 };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Page.Should().Be(999999);
    }

    [Fact]
    public void Parse_WithZeroAmounts_ShouldBeValid()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            MinAmount = 0m,
            MaxAmount = 0m
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.MinAmount.Should().Be(0m);
        result.Value.MaxAmount.Should().Be(0m);
    }

    [Fact]
    public void Parse_WithSameDateFromAndTo_ShouldBeValid()
    {
        // Arrange
        var request = new TransactionFilterRequest
        {
            DateFrom = "2024-06-15",
            DateTo = "2024-06-15"
        };

        // Act
        var result = TransactionFilterParser.Parse(request);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DateFrom.Should().Be(result.Value.DateTo);
    }

    #endregion
}
