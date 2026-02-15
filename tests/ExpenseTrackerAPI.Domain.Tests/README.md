# Domain Tests

Tests for domain entities and their validation rules.

## What We Test

### User Entity Tests
**File:** `Entities/UserTests.cs`

**Validation Tests:**
- ✅ Valid user creation with all fields
- ✅ Name validation (required, max 100 chars)
- ✅ Email validation (format, max 254 chars, no consecutive dots)
- ✅ Password hash validation (not empty)
- ✅ Email normalization (lowercase conversion)
- ✅ Name trimming
- ✅ UpdateProfile method
- ✅ UpdatePassword method
- ✅ UpdateInitialBalance method

**Edge Cases:**
- ❌ Empty/null/whitespace name
- ❌ Name exceeding 100 characters
- ❌ Invalid email formats (no @, no domain, consecutive dots, etc.)
- ❌ Email exceeding 254 characters
- ❌ Empty/null password hash

---

### Transaction Entity Tests
**File:** `Entities/TransactionTests.cs`

**Validation Tests:**
- ✅ Valid transaction creation (expense and income)
- ✅ User ID validation (positive integer)
- ✅ Amount validation (must be positive)
- ✅ Date validation (1900-01-01 to now+1year)
- ✅ Subject validation (required, max 255 chars)
- ✅ Category ID validation (positive if provided)
- ✅ Signed amount calculation (negative for expense, positive for income)
- ✅ Notes normalization (trim or null)

**Edge Cases:**
- ❌ Zero or negative user ID
- ❌ Zero or negative amount
- ❌ Date before 1900 or more than 1 year in future
- ❌ Empty/null subject
- ❌ Subject exceeding 255 characters
- ❌ Invalid transaction type
- ❌ Zero or negative category ID (when provided)

---

## Test Framework

- **xUnit** - Test runner
- **FluentAssertions** - Readable assertions
- **AAA Pattern** - Arrange, Act, Assert

## Example Test Structure

```csharp
[Fact]
public void Constructor_WithValidData_ShouldCreateUser()
{
    // Arrange
    var name = "John Doe";
    var email = "john@example.com";
    var passwordHash = "hashed_password";

    // Act
    var user = new User(name, email, passwordHash);

    // Assert
    user.Name.Should().Be("John Doe");
    user.Email.Should().Be("john@example.com");
}

[Fact]
public void Constructor_WithEmptyName_ShouldThrowArgumentException()
{
    // Arrange
    var name = "";

    // Act & Assert
    var action = () => new User(name, "email@test.com", "hash");
    action.Should().Throw<ArgumentException>()
        .WithMessage("*Name cannot be empty*");
}
```

## Running Tests

```bash
# Run all domain tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~UserTests"
```

## Key Points

- **Pure unit tests** - No database, no mocks, just entity logic
- **Fast execution** - Instant feedback on domain rules
- **Business rule documentation** - Tests serve as examples
- **Validation coverage** - All constructors and methods tested
- **Edge case handling** - Boundary conditions verified

---

## Related Documentation

### What We're Testing
- **[Domain Layer](../../src/ExpenseTrackerAPI.Domain/README.md)** - Entities and validation rules being tested

### Other Test Projects
- **[Application Tests](../ExpenseTrackerAPI.Application.Tests/README.md)** - Service layer tests
- **[Infrastructure Tests](../ExpenseTrackerAPI.Infrastructure.Tests/README.md)** - Repository tests
- **[WebApi Tests](../ExpenseTrackerAPI.WebApi.Tests/README.md)** - E2E API tests

### Architecture
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and testing strategy