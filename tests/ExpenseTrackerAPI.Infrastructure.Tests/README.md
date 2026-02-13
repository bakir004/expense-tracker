# ExpenseTrackerAPI.Infrastructure.Tests

This project contains integration tests for the Infrastructure layer, specifically testing repository implementations against a real PostgreSQL database using Testcontainers.

## Overview

These tests verify that:
- Database operations work correctly with actual database infrastructure
- Entity Framework Core mappings and migrations are correct
- **Cumulative delta calculations** are maintained correctly across all operations
- Transaction ordering (by Date, then CreatedAt) is preserved
- Repository error handling works as expected

## Technology Stack

- **xUnit** - Test framework
- **FluentAssertions** - Fluent assertion library for readable tests
- **Testcontainers** - Provides disposable PostgreSQL containers for isolated testing
- **Entity Framework Core** - ORM for database access
- **PostgreSQL** - Database engine (running in Docker containers)

## Prerequisites

- Docker must be installed and running on your machine
- .NET 9.0 SDK

Testcontainers will automatically:
- Pull the PostgreSQL Docker image if not present
- Start a new PostgreSQL container for each test run
- Clean up containers after tests complete

## Project Structure

```
Infrastructure.Tests/
├── Fixtures/
│   ├── DatabaseFixture.cs       # Manages PostgreSQL container lifecycle
│   └── DatabaseCollection.cs    # xUnit collection for sharing fixtures
├── Repositories/
│   ├── UserRepositoryTests.cs              # 11 tests
│   └── TransactionRepositoryTests.cs       # 15 tests
├── GlobalUsings.cs              # Global using directives
└── README.md
```

## Test Fixtures

### DatabaseFixture

The `DatabaseFixture` class:
- Manages the PostgreSQL container lifecycle
- Creates and configures the `ApplicationDbContext`
- Runs EF Core migrations to set up the database schema
- Provides methods to reset the database between tests
- Implements `IAsyncLifetime` for proper async initialization and disposal

**Key Methods:**
- `InitializeAsync()` - Starts container and runs migrations
- `ResetDatabaseAsync()` - Deletes and recreates database between tests
- `CreateNewContext()` - Creates a new DbContext instance for isolation
- `DisposeAsync()` - Cleans up container and context

### DatabaseCollection

The `DatabaseCollection` uses xUnit's `ICollectionFixture<T>` to share a single database container across multiple test classes, improving performance by avoiding repeated container startup/shutdown.

## Running Tests

### Run all infrastructure tests:
```bash
dotnet test tests/ExpenseTrackerAPI.Infrastructure.Tests
```

### Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~UserRepositoryTests"
dotnet test --filter "FullyQualifiedName~TransactionRepositoryTests"
```

### Run specific test:
```bash
dotnet test --filter "FullyQualifiedName~CreateAsync_NewTransactions_CumulativeDeltasFollowRule_ChronologicalOrder"
```

### With verbose output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run from solution root:
```bash
cd expense-tracker
dotnet test tests/ExpenseTrackerAPI.Infrastructure.Tests/ExpenseTrackerAPI.Infrastructure.Tests.csproj
```

## Test Categories

### UserRepository Tests (11 tests)

Tests cover:
- **CRUD Operations**: 
  - `CreateAsync_WithValidUser_ShouldCreateUser`
  - `GetByIdAsync_WithExistingUser_ShouldReturnUser`
  - `UpdateAsync_WithValidUser_ShouldUpdateUser`
  - `DeleteAsync_WithExistingUser_ShouldDeleteUser`
- **Query Operations**: 
  - `GetByEmailAsync_WithExistingEmail_ShouldReturnUser`
  - `ExistsByEmailAsync_WithExistingEmail_ShouldReturnTrue`
  - `GetAllAsync_WithMultipleUsers_ShouldReturnAllUsers`
- **Validation**: 
  - `CreateAsync_WithDuplicateEmail_ShouldReturnError`
- **Property Updates**:
  - `UpdateAsync_WithPasswordChange_ShouldUpdatePassword`
  - `UpdateAsync_WithInitialBalanceChange_ShouldUpdateBalance`
  - `CreateAsync_WithInitialBalance_ShouldCreateUserWithBalance`
- **Error Handling**: Not found, validation errors

### TransactionRepository Tests (15 tests)

**Critical Business Rule**: All tests verify the **Cumulative Delta Rule**:
```
When sorted by Date (ascending) then CreatedAt (ascending):
CumulativeDelta[i] = (i == 0 ? 0 : CumulativeDelta[i-1]) + SignedAmount[i]
```

This ensures that the running balance calculation is always correct.

Tests cover:

**1. Create Operations & Balance Calculations:**
- `CreateAsync_NewTransactions_CumulativeDeltasFollowRule_ChronologicalOrder` - Tests basic creation with multiple dates
- `CreateAsync_SameDateDifferentCreatedAt_CumulativeDeltasFollowRule` - Tests same-date ordering by CreatedAt
- `CreateAsync_SingleTransaction_FirstCumulativeEqualsSignedAmount` - Tests first transaction
- `CreateAsync_OnlyExpenses_NegativeCumulative_RuleHeld` - Tests negative cumulative deltas

**2. Update Operations & Recalculation:**
- `UpdateAsync_AmountOnly_AfterUpdate_RuleHeld` - Tests changing transaction amount
- `UpdateAsync_DateOnly_AfterUpdate_RuleHeld` - Tests changing transaction date (reordering)

**3. Delete Operations & Recalculation:**
- `DeleteAsync_AfterDelete_RemainingTransactionsFollowRule` - Tests deleting middle transaction
- `DeleteAsync_DeleteFirstTransaction_RemainingRuleHeld` - Tests deleting first transaction
- `DeleteAsync_DeleteLastTransaction_RemainingRuleHeld` - Tests deleting last transaction
- `CreateThenDeleteAll_UserHasZeroTransactions` - Tests complete deletion

**4. Complex Scenarios:**
- `Create_Update_Delete_Combine_RuleAlwaysHeld` - Tests mixed operations maintaining rule
- `CreateAsync_WithDifferentPaymentMethods_ShouldCreateSuccessfully` - Tests all payment methods
- `CreateAsync_WithDifferentUsers_ShouldMaintainSeparateCumulativeDeltas` - Tests multi-user isolation

**5. Error Cases:**
- `GetByIdAsync_ExistingTransaction_ReturnsTransaction` - Tests successful retrieval
- `GetByIdAsync_NonExistentTransaction_ReturnsError` - Tests not found error
- `DeleteAsync_NonExistentTransaction_ReturnsError` - Tests delete error handling

## Key Testing Patterns

### 1. Cumulative Delta Verification

The core assertion method used throughout transaction tests:

```csharp
private static void AssertCumulativeDeltaRule(List<Transaction> chronological)
{
    decimal previousCumulative = 0;
    for (var i = 0; i < chronological.Count; i++)
    {
        var current = chronological[i];
        var expectedCumulative = previousCumulative + current.SignedAmount;
        Assert.True(current.CumulativeDelta == expectedCumulative,
            $"At index {i}: expected {expectedCumulative}, but was {current.CumulativeDelta}");
        previousCumulative = current.CumulativeDelta;
    }
}
```

### 2. Database Reset Between Tests

Each test resets the database to ensure isolation:

```csharp
await _fixture.ResetDatabaseAsync();
```

This ensures tests don't interfere with each other and can run in any order.

### 3. Test User Creation Helper

Transaction tests use a helper method to create test users:

```csharp
private async Task<User> CreateTestUserAsync()
{
    var user = new User(
        name: "Integration Test User",
        email: $"test-{Guid.NewGuid():N}@integration.test",
        passwordHash: "hashed_password",
        initialBalance: 0
    );
    var result = await _userRepository.CreateAsync(user, CancellationToken.None);
    return result.Value;
}
```

### 4. Transaction Creation Helper

Creates properly structured transactions for testing:

```csharp
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
        createdAt: createdAt,
        updatedAt: createdAt
    );
}
```

### 5. Chronological Sorting

Ensures transactions are ordered correctly for balance calculations:

```csharp
private static List<Transaction> ChronologicalForUser(List<Transaction> forUser)
{
    return forUser
        .OrderBy(t => t.Date)
        .ThenBy(t => t.CreatedAt)
        .ToList();
}
```

### 6. Arrange-Act-Assert Pattern

Tests follow the standard testing pattern:

```csharp
// Arrange
await _fixture.ResetDatabaseAsync();
var user = await CreateTestUserAsync();
var transaction = NewTransaction(user.Id, 100m, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow);

// Act
var result = await _sut.CreateAsync(transaction, CancellationToken.None);

// Assert
result.IsError.Should().BeFalse();
result.Value.CumulativeDelta.Should().Be(100m);
```

## Domain Model Notes

### Transaction Entity

Key properties tested:
- `Date` (DateOnly) - Transaction date without time
- `Subject` (string) - Brief description
- `Amount` (decimal) - Absolute amount (always positive)
- `SignedAmount` (decimal) - Negative for expenses, positive for income
- `CumulativeDelta` (decimal) - Running sum of all signed amounts
- `TransactionType` - INCOME or EXPENSE
- `PaymentMethod` - CASH, CREDIT_CARD, DEBIT_CARD, etc.
- `CreatedAt` (DateTime) - Used for ordering transactions on same date

### User Entity

Key properties tested:
- `Name` (string) - User's display name
- `Email` (string) - Unique email address
- `PasswordHash` (string) - Hashed password
- `InitialBalance` (decimal) - Starting balance

## Test Data

- All tests use isolated data created within each test method
- Email addresses use GUIDs to prevent collisions: `$"test-{Guid.NewGuid():N}@integration.test"`
- Dates use `DateOnly.FromDateTime(DateTime.UtcNow)` with `.AddDays()` offsets
- CreatedAt timestamps use `DateTime.UtcNow` with `.AddSeconds()` offsets for ordering
- Monetary amounts use realistic decimal values

## Performance Considerations

- Database container is shared across test classes in the same collection (~3-5 seconds startup)
- Each test resets the database rather than creating a new container (~100-200ms per reset)
- Tests run sequentially within a collection to avoid database conflicts
- Total test execution time: ~10-15 seconds for all 26 tests

## Troubleshooting

### Docker not running
**Error**: "Cannot connect to Docker daemon"  
**Solution**: Start Docker Desktop or Docker daemon
```bash
sudo systemctl start docker  # Linux
# or open Docker Desktop on Windows/Mac
```

### Port conflicts
**Error**: "Port already in use"  
**Solution**: Testcontainers automatically assigns random available ports. No action needed.

### Container cleanup issues
**Solution**: Manually clean up orphaned containers:
```bash
docker ps -a | grep postgres | awk '{print $1}' | xargs docker rm -f
docker ps -a | grep testcontainers | awk '{print $1}' | xargs docker rm -f
```

### Migration issues
**Error**: "Pending model changes detected"  
**Solution**: Create and apply new migration:
```bash
cd src/ExpenseTrackerAPI.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../ExpenseTrackerAPI.WebApi
dotnet ef database update --startup-project ../ExpenseTrackerAPI.WebApi
```

### Test failures due to time zones
**Solution**: Tests use `DateTime.UtcNow` consistently. Ensure system clock is correct.

## CI/CD Integration

These tests are designed to run in CI/CD pipelines that support Docker:

### GitHub Actions
```yaml
- name: Run Integration Tests
  run: dotnet test tests/ExpenseTrackerAPI.Infrastructure.Tests
```

### GitLab CI
```yaml
test:
  image: mcr.microsoft.com/dotnet/sdk:9.0
  services:
    - docker:dind
  script:
    - dotnet test tests/ExpenseTrackerAPI.Infrastructure.Tests
```

### Azure DevOps
Ensure Docker capability is available on the build agent.

**Requirements:**
- Docker runtime available
- Sufficient permissions to start containers
- Adequate resources (minimum 2GB RAM, 2 CPU cores recommended)

## Best Practices

1. **Always reset the database** at the start of each test for isolation
2. **Use unique data** (GUIDs in emails) to prevent conflicts
3. **Test both success and error paths** for each operation
4. **Verify side effects** - After operations, verify cumulative deltas are recalculated
5. **Keep tests independent** - No shared state between tests
6. **Use descriptive test names** - Follow pattern: `MethodName_Scenario_ExpectedResult`
7. **Verify the cumulative delta rule** - Every transaction test should verify balances
8. **Test edge cases** - First transaction, last transaction, same-date ordering
9. **Test multi-user scenarios** - Ensure users don't affect each other's balances

## Common Test Scenarios

### Testing Balance Recalculation After Update
```csharp
// Create 3 transactions: +100, -30, +50 = 120
// Update middle to -40
// Expected: +100, -40, +50 = 110
// Verify last transaction's cumulative delta is recalculated
```

### Testing Same-Date Transaction Ordering
```csharp
// Create transactions on same date with different CreatedAt
// Verify they're ordered by CreatedAt
// Verify cumulative deltas respect this ordering
```

### Testing Multi-User Isolation
```csharp
// Create User1 with +500 transaction
// Create User2 with +300 transaction
// Verify User1's cumulative = 500 (not 800)
// Verify User2's cumulative = 300 (not affected by User1)
```

## Future Enhancements

Potential improvements:
- Add performance benchmarks for repository operations
- Test concurrent operations and race conditions
- Add tests for transaction notes and categories
- Test complex date range queries
- Add tests for transaction groups
- Test bulk operations (create/update/delete multiple)
- Add snapshot testing for database state
- Test migration rollback scenarios
- Add stress tests with thousands of transactions

## Related Documentation

- [Domain Layer](../../src/ExpenseTrackerAPI.Domain/README.md) - Entity definitions and business rules
- [Infrastructure Layer](../../src/ExpenseTrackerAPI.Infrastructure/README.md) - Repository implementations
- [Application Layer Tests](../ExpenseTrackerAPI.Application.Tests/README.md) - Service layer tests

## Test Statistics

- **Total Tests**: 26 (11 UserRepository + 15 TransactionRepository)
- **Execution Time**: ~10-15 seconds (including container startup)
- **Code Coverage**: Covers all public repository methods
- **Database Operations**: Tests all CRUD operations plus complex balance calculations