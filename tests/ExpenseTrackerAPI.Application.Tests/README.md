# Application Tests

Tests for application services using mocked repositories.

## What We Test

### User Service Tests
**File:** `Users/UserServiceTests.cs`

**Core Operations:**
- ✅ Register user (password hashing, validation)
- ✅ Login user (password verification, JWT token generation)
- ✅ Update user profile (partial updates, password changes)
- ✅ Delete user account (confirmation, cascade)

**Test Scenarios:**
- ✅ Valid registration with initial balance
- ✅ Duplicate email detection
- ✅ Password hashing verification
- ✅ Successful login with correct credentials
- ✅ Failed login with wrong password
- ✅ Update single field (partial update)
- ✅ Update multiple fields at once
- ✅ Email uniqueness check on update
- ✅ Delete with confirmation flag

---

### Transaction Service Tests
**File:** `Transactions/TransactionServiceFilteringTests.cs`

**Core Operations:**
- ✅ Create transaction
- ✅ Update transaction
- ✅ Delete transaction
- ✅ Query with filters

**Filtering Tests:**
- ✅ Filter by transaction type (EXPENSE/INCOME)
- ✅ Filter by amount range
- ✅ Filter by date range
- ✅ Filter by payment methods
- ✅ Filter by categories
- ✅ Filter uncategorized transactions
- ✅ Sort by date, amount, subject
- ✅ Pagination (page, page size)
- ✅ Combined filters

---

### Transaction Group Service Tests
**File:** `TransactionGroups/TransactionGroupServiceTests.cs`

**Core Operations:**
- ✅ Create transaction group
- ✅ Update transaction group
- ✅ Delete transaction group
- ✅ Get groups by user

---

### Category Service Tests
**File:** `Categories/CategoryServiceTests.cs`

**Core Operations:**
- ✅ Get all categories
- ✅ Get category by ID

---

## Test Framework

- **xUnit** - Test runner
- **Moq** - Mocking framework for repositories
- **FluentAssertions** - Readable assertions
- **ErrorOr** - Functional error handling

## Example Test Structure

```csharp
[Fact]
public async Task RegisterAsync_WithValidData_ShouldSucceed()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new User(...));
    
    var service = new UserService(mockRepo.Object, mockTokenGenerator.Object);
    var request = new RegisterRequest(...);

    // Act
    var result = await service.RegisterAsync(request, CancellationToken.None);

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Name.Should().Be("John Doe");
}
```

## Key Points

- **Unit tests** - Services isolated with mocked dependencies
- **No database** - All repository calls mocked
- **ErrorOr testing** - Verify success/failure paths
- **Security validation** - Password hashing, token generation
- **Business logic** - Service orchestration and validation

## Running Tests

```bash
# Run all application tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~UserServiceTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

---

## Related Documentation

### What We're Testing
- **[Application Layer](../../src/ExpenseTrackerAPI.Application/README.md)** - Services being tested

### Other Test Projects
- **[Domain Tests](../ExpenseTrackerAPI.Domain.Tests/README.md)** - Entity validation tests
- **[Infrastructure Tests](../ExpenseTrackerAPI.Infrastructure.Tests/README.md)** - Repository tests
- **[WebApi Tests](../ExpenseTrackerAPI.WebApi.Tests/README.md)** - E2E API tests

### Architecture
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and testing strategy
