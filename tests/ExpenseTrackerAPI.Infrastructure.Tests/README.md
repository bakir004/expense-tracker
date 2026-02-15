# Infrastructure Tests

Tests for repositories and database operations using real PostgreSQL database.

## What We Test

### User Repository Tests
**File:** `Repositories/UserRepositoryTests.cs`

**Core Operations:**
- ✅ Create user
- ✅ Get user by ID
- ✅ Get user by email
- ✅ Check email existence
- ✅ Update user
- ✅ Delete user (cascade to transactions)

**Test Scenarios:**
- ✅ Duplicate email constraint
- ✅ Email case-insensitivity
- ✅ User not found errors
- ✅ Cascade delete to related transactions

---

### Transaction Repository Tests
**File:** `Repositories/TransactionRepositoryTests.cs`

**Core Operations:**
- ✅ Create transaction (updates cumulative deltas)
- ✅ Update transaction (recalculates deltas)
- ✅ Delete transaction (adjusts deltas)
- ✅ Get transaction by ID

**Cumulative Delta Tests:**
- ✅ Initial transaction sets delta correctly
- ✅ Sequential creates maintain running balance
- ✅ Out-of-order inserts recalculate correctly
- ✅ Amount change updates subsequent transactions
- ✅ Date change forward in time
- ✅ Date change backward in time
- ✅ Delete adjusts subsequent deltas
- ✅ Multiple transactions on same date

**Filtering Tests (separate file):**
**File:** `Repositories/TransactionRepositoryFilteringTests.cs`
- ✅ Filter by type, amount range, date range
- ✅ Filter by payment methods, categories, groups
- ✅ Search in subject and notes
- ✅ Sort by various fields (date, amount, subject)
- ✅ Pagination with page and page size
- ✅ Combined filters

---

### Transaction Group Repository Tests
**File:** `Repositories/TransactionGroupRepositoryTests.cs`

**Core Operations:**
- ✅ Create transaction group
- ✅ Get group by ID
- ✅ Get groups by user ID
- ✅ Update transaction group
- ✅ Delete transaction group

**Test Scenarios:**
- ✅ User isolation (can't access other user's groups)
- ✅ Delete sets transactions to null (SetNull behavior)

---

### Category Repository Tests
**File:** `Repositories/CategoryRepositoryTests.cs`

**Core Operations:**
- ✅ Get all categories
- ✅ Get category by ID

---

## Test Setup

### Database Fixture
**File:** `Fixtures/DatabaseFixture.cs`

- Uses **Testcontainers** to spin up PostgreSQL in Docker
- Creates fresh database for each test class
- Runs migrations automatically
- Cleans up after tests complete

### Test Base Class
- Provides clean database context per test
- Handles transaction rollback
- Seeds test data when needed

---

## Test Framework

- **xUnit** - Test runner
- **Testcontainers** - PostgreSQL in Docker
- **FluentAssertions** - Readable assertions
- **EF Core** - Real database operations

## Example Test Structure

```csharp
[Fact]
public async Task CreateAsync_ShouldCalculateCumulativeDelta()
{
    // Arrange
    var user = await CreateTestUser();
    var transaction = new Transaction(
        userId: user.Id,
        transactionType: TransactionType.EXPENSE,
        amount: 100m,
        date: DateOnly.FromDateTime(DateTime.Now),
        subject: "Test",
        paymentMethod: PaymentMethod.CASH
    );

    // Act
    var result = await _repository.CreateAsync(transaction, CancellationToken.None);

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.CumulativeDelta.Should().Be(-100m);
}
```

## Key Points

- **Integration tests** - Real database, real queries
- **Testcontainers** - PostgreSQL in Docker, isolated per test class
- **Cumulative delta verification** - Core business logic tested
- **Foreign key constraints** - Database integrity validated
- **Performance** - Bulk update operations tested
- **Concurrency** - Transaction isolation verified

## Running Tests

```bash
# Requires Docker running
docker --version

# Run all infrastructure tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~TransactionRepositoryTests"
```

## Prerequisites

- Docker running (for Testcontainers)
- PostgreSQL image will be pulled automatically
- Tests create temporary databases (cleaned up automatically)

---

## Related Documentation

### What We're Testing
- **[Infrastructure Layer](../../src/ExpenseTrackerAPI.Infrastructure/README.md)** - Repositories and cumulative delta system being tested

### Other Test Projects
- **[Domain Tests](../ExpenseTrackerAPI.Domain.Tests/README.md)** - Entity validation tests
- **[Application Tests](../ExpenseTrackerAPI.Application.Tests/README.md)** - Service layer tests
- **[WebApi Tests](../ExpenseTrackerAPI.WebApi.Tests/README.md)** - E2E API tests

### Architecture
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and testing strategy