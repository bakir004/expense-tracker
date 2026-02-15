# WebApi Tests

End-to-end API tests using in-memory test server with real database.

## What We Test

### Authentication Tests
**File:** `E2E/Auth/AuthApiTests.cs`

**Core Operations:**
- ✅ Register new user
- ✅ Login with credentials
- ✅ JWT token generation
- ✅ Token expiration included in response

**Test Scenarios:**
- ✅ Successful registration with valid data
- ✅ Duplicate email rejection
- ✅ Successful login returns token
- ✅ Invalid credentials rejected
- ✅ Password complexity enforcement

---

### User API Tests
**File:** `E2E/Users/UserApiTests.cs`

**Core Operations:**
- ✅ Update user profile (requires authentication)
- ✅ Delete user account (requires authentication)
- ✅ Partial update support

**Test Scenarios:**
- ✅ Update single field (name, email, password, balance)
- ✅ Update multiple fields at once
- ✅ Password verification required
- ✅ Email uniqueness checked
- ✅ Delete with confirmation flag
- ✅ Unauthorized access rejected

---

### Transaction API Tests
**File:** `E2E/Transactions/TransactionApiTests.cs`

**Core Operations:**
- ✅ Create transaction
- ✅ Get transaction by ID
- ✅ Update transaction
- ✅ Delete transaction
- ✅ Query with filters and pagination

**Test Scenarios:**
- ✅ Create expense and income
- ✅ Filter by type, date range, amount range
- ✅ Filter by payment methods, categories, groups
- ✅ Sort by date, amount, subject
- ✅ Pagination with page/pageSize
- ✅ Authorization checks (can't access other user's transactions)

---

### Error Format Tests
**Files:** `E2E/Errors/*ErrorFormatTests.cs`

**What We Test:**
- ✅ RFC 7807 Problem Details format
- ✅ Validation errors structure
- ✅ Error message clarity
- ✅ HTTP status codes (400, 401, 404, 409, etc.)
- ✅ No sensitive data in errors

**Test Files:**
- `AuthErrorFormatTests.cs` - Registration/login errors
- `UserErrorFormatTests.cs` - Profile update/delete errors
- `TransactionErrorFormatTests.cs` - Transaction CRUD errors
- `TransactionGroupErrorFormatTests.cs` - Group operation errors

---

### Workflow Tests
**Files:** `E2E/UserWorkflows/*WorkflowTests.cs`

**Complete User Journey:**
**File:** `CompleteUserJourneyTests.cs`
- ✅ Register → Login → Create transactions → Update profile → Delete account

**User Registration Workflow:**
**File:** `UserRegistrationWorkflowTests.cs`
- ✅ Multi-step registration scenarios
- ✅ Validation at each step
- ✅ Error recovery

**User Transaction Workflow:**
**File:** `UserTransactionWorkflowTests.cs`
- ✅ Create multiple transactions
- ✅ Query and filter
- ✅ Update and delete
- ✅ Verify balance calculations

**Transaction Group Workflow:**
**File:** `TransactionGroupWorkflowTests.cs`
- ✅ Create groups
- ✅ Assign transactions to groups
- ✅ Query by group
- ✅ Delete groups (transactions ungrouped)

---

## Test Setup

### WebApplicationFactory
**File:** `Fixtures/WebApiFactory.cs`

- Uses **WebApplicationFactory** for in-memory test server
- Real ASP.NET Core pipeline (middleware, filters, routing)
- **Testcontainers** for PostgreSQL database
- Fresh database per test class
- Automatic cleanup

### Test Constants
**File:** `Common/TestConstants.cs`

- API routes
- Seeded test users
- Common test data
- HTTP client configuration

### Authentication Helper
**File:** `Common/AuthHelper.cs`

- Login and get JWT token
- Attach token to requests
- Test user creation

---

## Test Framework

- **xUnit** - Test runner
- **WebApplicationFactory** - In-memory test server
- **Testcontainers** - PostgreSQL in Docker
- **FluentAssertions** - Readable assertions
- **HttpClient** - API calls

## Example Test Structure

```csharp
[Fact]
public async Task Register_WithValidData_ShouldReturn201()
{
    // Arrange
    var request = new RegisterRequest(
        Name: "John Doe",
        Email: "john@example.com",
        Password: "Password123!",
        InitialBalance: 1000m
    );

    // Act
    var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
    content.Name.Should().Be("John Doe");
}
```

## Key Points

- **E2E tests** - Full HTTP request/response cycle
- **Real database** - PostgreSQL in Docker
- **Real authentication** - JWT tokens required
- **Real middleware** - CORS, validation, error handling
- **Workflow tests** - Multi-step user journeys
- **Error format validation** - RFC 7807 compliance

## Running Tests

```bash
# Requires Docker running
docker --version

# Run all WebApi tests
dotnet test

# Run specific category
dotnet test --filter "FullyQualifiedName~AuthApiTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Categories

- **API Tests** - Individual endpoint tests
- **Error Tests** - Error response format validation
- **Workflow Tests** - Multi-step scenarios
- **Integration** - Full stack with database

## Prerequisites

- Docker running (for Testcontainers)
- PostgreSQL image pulled automatically
- Tests run in parallel by test class
- Each test class gets isolated database

---

## Related Documentation

### What We're Testing
- **[WebApi Layer](../../src/ExpenseTrackerAPI.WebApi/README.md)** - API endpoints being tested

### Other Test Projects
- **[Domain Tests](../ExpenseTrackerAPI.Domain.Tests/README.md)** - Entity validation tests
- **[Application Tests](../ExpenseTrackerAPI.Application.Tests/README.md)** - Service layer tests
- **[Infrastructure Tests](../ExpenseTrackerAPI.Infrastructure.Tests/README.md)** - Repository tests

### Architecture
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and testing strategy