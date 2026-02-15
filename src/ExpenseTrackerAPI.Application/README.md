# ExpenseTracker Application Layer

This document explains the Application layer of the ExpenseTracker API, which contains business logic, service orchestration, and coordinates between the domain and infrastructure layers.

## Table of Contents

- [Overview](#overview)
- [Layer Responsibilities](#layer-responsibilities)
- [Architecture](#architecture)
- [User Service](#user-service)
- [Transaction Service](#transaction-service)
- [Service Patterns](#service-patterns)
- [Error Handling](#error-handling)
- [Security Considerations](#security-considerations)

---

## Overview

The Application layer orchestrates business workflows and coordinates between layers. It:

- ✅ Implements business use cases
- ✅ Orchestrates domain entities and repositories
- ✅ Handles cross-cutting concerns (authentication, validation)
- ✅ Coordinates transactions and data persistence
- ✅ Transforms between contracts and domain entities
- ✅ Enforces business rules beyond entity validation

**Key Principle:** The Application layer is the entry point for all business operations. It orchestrates the domain without containing domain logic itself.

### Layer Structure

```
Application/
├── Users/
│   ├── UserService.cs                    # User business operations
│   └── Interfaces/
│       ├── Application/
│       │   └── IUserService.cs           # Service contract
│       └── Infrastructure/
│           └── IUserRepository.cs        # Repository contract
├── Transactions/
│   ├── TransactionService.cs             # Transaction operations
│   └── Interfaces/
│       ├── Application/
│       │   └── ITransactionService.cs
│       └── Infrastructure/
│           └── ITransactionRepository.cs
├── TransactionGroups/
│   ├── TransactionGroupService.cs
│   └── Interfaces/...
├── Categories/
│   ├── CategoryService.cs
│   └── Interfaces/...
└── Common/
    └── Interfaces/
        ├── IJwtTokenGenerator.cs
        └── IDateTimeProvider.cs
```

---

## Layer Responsibilities

### What the Application Layer Does

✅ **Orchestration:**
- Coordinates multiple domain entities
- Manages workflow across repositories
- Handles business transactions

✅ **Validation:**
- Password complexity validation
- Email uniqueness checking
- Authorization checks
- Cross-entity validation

✅ **Security:**
- Password hashing (BCrypt)
- JWT token generation
- Current user verification
- Permission checks

✅ **Transformation:**
- Converts contracts to domain entities
- Maps domain entities to responses
- Handles parsing and formatting

✅ **Error Handling:**
- Catches domain exceptions
- Returns typed errors (ErrorOr)
- Provides meaningful error messages

### What the Application Layer Does NOT Do

❌ **Domain Logic:**
- Business rules (handled by domain entities)
- Entity validation (handled by entity constructors)
- Calculated properties (handled by domain)

❌ **Infrastructure Concerns:**
- Database access (handled by repositories)
- HTTP requests/responses (handled by controllers)
- Configuration management

---

## Architecture

### Dependency Flow

```
WebApi (Controllers)
    ↓
Application (Services) ← You are here
    ↓
Domain (Entities & Business Rules)
    ↓
Infrastructure (Repositories & Data Access)
```

### Service Pattern

```csharp
public interface IUserService
{
    Task<ErrorOr<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    // ... other operations
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    // Implementation
}
```

**Key Characteristics:**
- Interface-based design
- Constructor dependency injection
- ErrorOr return type for functional error handling
- Async/await for all I/O operations

---

## User Service

**Location:** `Users/UserService.cs`

**Purpose:** Manages user account operations including registration, authentication, and profile management.

### Operations

#### 1. Register User

**Method:** `RegisterAsync(RegisterRequest request, CancellationToken ct)`

**Workflow:**
1. Hash the plain password using BCrypt
2. Create domain User entity (validates business rules)
3. Call repository to persist user
4. Return RegisterResponse DTO

**Code Flow:**
```csharp
public async Task<ErrorOr<RegisterResponse>> RegisterAsync(
    RegisterRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        // 1. Hash password (Application layer responsibility)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(
            request.Password,
            BCrypt.Net.BCrypt.GenerateSalt()
        );

        // 2. Create domain entity (validates business rules)
        var user = new User(
            name: request.Name,
            email: request.Email,
            passwordHash: passwordHash,
            initialBalance: request.InitialBalance ?? 0
        );

        // 3. Persist to database
        var createResult = await _userRepository.CreateAsync(user, cancellationToken);
        if (createResult.IsError)
            return createResult.Errors;

        // 4. Transform to response DTO
        var createdUser = createResult.Value;
        return new RegisterResponse(
            Id: createdUser.Id,
            Name: createdUser.Name,
            Email: createdUser.Email,
            InitialBalance: createdUser.InitialBalance,
            CreatedAt: createdUser.CreatedAt
        );
    }
    catch (Exception ex)
    {
        return UserErrors.RegistrationFailed(ex.Message);
    }
}
```

**Key Points:**
- ✅ Password is hashed before creating domain entity
- ✅ Domain entity validates business rules (name, email format)
- ✅ Repository handles uniqueness checking
- ✅ Exceptions are caught and converted to ErrorOr
- ✅ Password is never exposed in response

---

#### 2. Login User

**Method:** `LoginAsync(LoginRequest request, CancellationToken ct)`

**Workflow:**
1. Retrieve user by email
2. Verify password using BCrypt
3. Generate JWT token
4. Return LoginResponse with token

**Code Flow:**
```csharp
public async Task<ErrorOr<LoginResponse>> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        // 1. Get user by email
        var userResult = await _userRepository.GetByEmailAsync(
            request.Email,
            cancellationToken
        );

        if (userResult.IsError)
        {
            return UserErrors.InvalidCredentials;  // Don't reveal if user exists
        }

        var user = userResult.Value;

        // 2. Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return UserErrors.InvalidCredentials;  // Same error for security
        }

        // 3. Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(
            user.Id,
            user.Email,
            user.Name
        );

        var expiresAt = DateTime.UtcNow.AddHours(
            _jwtTokenGenerator.TokenExpirationHours
        );

        // 4. Return response with token
        return new LoginResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email,
            InitialBalance: user.InitialBalance,
            Token: token,
            ExpiresAt: expiresAt
        );
    }
    catch (Exception ex)
    {
        return UserErrors.LoginFailed(ex.Message);
    }
}
```

**Security Notes:**
- ✅ Same error message for "user not found" and "wrong password"
- ✅ BCrypt for password verification
- ✅ JWT token with expiration
- ✅ No password in response

---

#### 3. Update User Profile

**Method:** `UpdateAsync(int userId, UpdateUserRequest request, CancellationToken ct)`

**Workflow:**
1. Retrieve existing user
2. Verify current password
3. Check email uniqueness (if changing email)
4. Validate and hash new password (if provided)
5. Update only provided fields (partial update)
6. Persist changes
7. Return updated user details

**Code Flow:**
```csharp
public async Task<ErrorOr<UpdateUserResponse>> UpdateAsync(
    int userId,
    UpdateUserRequest request,
    CancellationToken cancellationToken)
{
    if (userId <= 0)
        return UserErrors.InvalidUserId;

    try
    {
        // 1. Get existing user
        var userResult = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
            return userResult.Errors;

        var user = userResult.Value;

        // 2. Verify current password (security check)
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return UserErrors.InvalidCredentials;
        }

        // 3. Check email uniqueness (if changing)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var existsResult = await _userRepository.ExistsByEmailAsync(
                    request.Email,
                    cancellationToken
                );

                if (existsResult.IsError)
                    return existsResult.Errors;

                if (existsResult.Value)
                    return UserErrors.DuplicateEmail;
            }
        }

        // 4. Update password if provided
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            // Validate password complexity (Application layer responsibility)
            if (!HasValidPasswordComplexity(request.NewPassword))
                return UserErrors.WeakPassword;

            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword,
                BCrypt.Net.BCrypt.GenerateSalt()
            );
            user.UpdatePassword(newPasswordHash);
        }

        // 5. Update name and email (use existing if not provided)
        var nameToUpdate = request.Name ?? user.Name;
        var emailToUpdate = request.Email ?? user.Email;
        user.UpdateProfile(nameToUpdate, emailToUpdate);

        // 6. Update balance if provided
        if (request.InitialBalance.HasValue)
        {
            user.UpdateInitialBalance(request.InitialBalance.Value);
        }

        // 7. Persist changes
        var updateResult = await _userRepository.UpdateAsync(user, cancellationToken);
        if (updateResult.IsError)
            return updateResult.Errors;

        var updatedUser = updateResult.Value;

        return new UpdateUserResponse(
            Id: updatedUser.Id,
            Name: updatedUser.Name,
            Email: updatedUser.Email,
            InitialBalance: updatedUser.InitialBalance,
            UpdatedAt: updatedUser.UpdatedAt
        );
    }
    catch (ArgumentException)
    {
        return UserErrors.InvalidEmail;
    }
    catch (Exception ex)
    {
        return UserErrors.UpdateFailed(ex.Message);
    }
}
```

**Partial Update Pattern:**
- ✅ Only `CurrentPassword` is required
- ✅ Null fields are ignored (keep existing values)
- ✅ Allows updating individual fields
- ✅ Security: always requires password verification

**Password Complexity Validation:**
```csharp
private static bool HasValidPasswordComplexity(string password)
{
    if (string.IsNullOrWhiteSpace(password))
        return false;

    var hasUppercase = password.Any(char.IsUpper);
    var hasLowercase = password.Any(char.IsLower);
    var hasDigit = password.Any(char.IsDigit);
    var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

    return hasUppercase && hasLowercase && hasDigit && hasSpecialChar;
}
```

---

#### 4. Delete User Account

**Method:** `DeleteAsync(int userId, DeleteUserRequest request, CancellationToken ct)`

**Workflow:**
1. Validate user ID
2. Verify password
3. Check deletion confirmation flag
4. Delete user (cascades to transactions)
5. Return confirmation

**Code Flow:**
```csharp
public async Task<ErrorOr<DeleteUserResponse>> DeleteAsync(
    int userId,
    DeleteUserRequest request,
    CancellationToken cancellationToken)
{
    if (userId <= 0)
        return UserErrors.InvalidUserId;

    if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        return UserErrors.PasswordRequired;

    // Explicit confirmation required
    if (!request.ConfirmDeletion)
        return UserErrors.DeletionConfirmation;

    try
    {
        // Get user
        var userResult = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
            return userResult.Errors;

        var user = userResult.Value;

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return UserErrors.InvalidCredentials;
        }

        // Delete (cascades to transactions)
        var deleteResult = await _userRepository.DeleteAsync(userId, cancellationToken);
        if (deleteResult.IsError)
            return deleteResult.Errors;

        return new DeleteUserResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email,
            Message: "User account has been permanently deleted."
        );
    }
    catch (Exception ex)
    {
        return UserErrors.DeleteFailed(ex.Message);
    }
}
```

**Security Checks:**
- ✅ Password verification
- ✅ Explicit confirmation flag
- ✅ User must authenticate themselves
- ✅ Permanent action with clear messaging

---

## Transaction Service

**Location:** `Transactions/TransactionService.cs`

**Purpose:** Manages transaction operations including creation, updates, deletion, and querying.

### Operations

#### 1. Create Transaction

**Method:** `CreateAsync(...)`

**Workflow:**
1. Create domain Transaction entity (validates business rules)
2. Call repository to persist and update cumulative deltas
3. Return transaction response

**Code Flow:**
```csharp
public async Task<ErrorOr<Transaction>> CreateAsync(
    int userId,
    TransactionType transactionType,
    decimal amount,
    DateOnly date,
    string subject,
    string? notes,
    PaymentMethod paymentMethod,
    int? categoryId,
    int? transactionGroupId,
    CancellationToken cancellationToken)
{
    try
    {
        // Create domain entity (validates business rules)
        var transaction = new Transaction(
            userId: userId,
            transactionType: transactionType,
            amount: amount,
            date: date,
            subject: subject,
            paymentMethod: paymentMethod,
            notes: notes,
            categoryId: categoryId,
            transactionGroupId: transactionGroupId
        );

        // Repository handles persistence and cumulative delta calculations
        return await _transactionRepository.CreateAsync(transaction, cancellationToken);
    }
    catch (ArgumentException ex)
    {
        return TransactionErrors.ValidationError(ex.Message);
    }
}
```

**Key Points:**
- ✅ Domain entity validates business rules
- ✅ Repository handles cumulative delta calculations
- ✅ ArgumentException from domain converted to ErrorOr
- ✅ Simple orchestration - domain does the work

---

#### 2. Update Transaction

**Method:** `UpdateAsync(...)`

**Workflow:**
1. Retrieve existing transaction
2. Verify ownership (authorization)
3. Create new transaction with updated values
4. Call repository to update and recalculate deltas
5. Return updated transaction

**Code Flow:**
```csharp
public async Task<ErrorOr<Transaction>> UpdateAsync(
    int id,
    int userId,
    TransactionType transactionType,
    decimal amount,
    DateOnly date,
    string subject,
    string? notes,
    PaymentMethod paymentMethod,
    int? categoryId,
    int? transactionGroupId,
    CancellationToken cancellationToken)
{
    try
    {
        // 1. Get existing transaction
        var oldTransaction = await _transactionRepository.GetByIdAsync(
            id,
            userId,
            cancellationToken
        );

        if (oldTransaction.IsError)
        {
            return oldTransaction.Errors;
        }

        var existingTransaction = oldTransaction.Value;

        // 2. Authorization check
        if (userId != existingTransaction.UserId)
            return TransactionErrors.Unauthorized;

        // 3. Create updated transaction (validates business rules)
        var updatedTransaction = new Transaction(
            userId: existingTransaction.UserId,
            transactionType: transactionType,
            amount: amount,
            date: date,
            subject: subject,
            paymentMethod: paymentMethod,
            notes: notes,
            categoryId: categoryId,
            transactionGroupId: transactionGroupId
        );

        updatedTransaction.UpdateId(id);

        // 4. Repository handles update and delta recalculation
        return await _transactionRepository.UpdateAsync(
            existingTransaction,
            updatedTransaction,
            cancellationToken
        );
    }
    catch (ArgumentException ex)
    {
        return TransactionErrors.ValidationError(ex.Message);
    }
}
```

**Authorization:**
- ✅ Verify transaction belongs to user
- ✅ Return generic error (don't reveal existence)
- ✅ Prevents cross-user access

---

#### 3. Delete Transaction

**Method:** `DeleteAsync(int id, int userId, CancellationToken ct)`

**Workflow:**
1. Retrieve transaction
2. Verify ownership
3. Call repository to delete and recalculate deltas
4. Return success

**Code Flow:**
```csharp
public async Task<ErrorOr<Deleted>> DeleteAsync(
    int id,
    int userId,
    CancellationToken cancellationToken)
{
    // 1. Get transaction
    var existingTransaction = await _transactionRepository.GetByIdAsync(
        id,
        userId,
        cancellationToken
    );

    if (existingTransaction.IsError)
        return existingTransaction.Errors;

    // 2. Authorization check
    var transactionOwnerId = existingTransaction.Value.UserId;

    if (transactionOwnerId != userId)
        return TransactionErrors.NotFound;  // Don't reveal existence

    // 3. Repository handles deletion and delta recalculation
    return await _transactionRepository.DeleteAsync(id, cancellationToken);
}
```

---

#### 4. Query Transactions with Filters

**Method:** `GetByUserIdWithFilterAsync(...)`

**Workflow:**
1. Verify user exists
2. Call repository with filter criteria
3. Build paginated response
4. Return transaction list with metadata

**Code Flow:**
```csharp
public async Task<ErrorOr<TransactionFilterResponse>> GetByUserIdWithFilterAsync(
    int userId,
    TransactionFilter filter,
    CancellationToken cancellationToken)
{
    // 1. Verify user exists
    var userResult = await _userRepository.GetByIdAsync(userId, cancellationToken);
    if (userResult.IsError)
    {
        return UserErrors.NotFound;
    }

    // 2. Get filtered transactions
    var result = await _transactionRepository.GetByUserIdWithFilterAsync(
        userId,
        filter,
        cancellationToken
    );

    if (result.IsError)
    {
        return result.Errors;
    }

    var transactions = result.Value;

    // 3. Build paginated response
    return new TransactionFilterResponse
    {
        Transactions = transactions.Select(t => t.ToResponse()).ToList(),
        TotalCount = transactions.Count,
        Page = filter.Page,
        PageSize = filter.PageSize,
        TotalPages = (int)Math.Ceiling(transactions.Count / (double)filter.PageSize)
    };
}
```

---

## Service Patterns

### 1. ErrorOr Pattern

All services use `ErrorOr<T>` for functional error handling:

```csharp
// Success case
return new RegisterResponse(...);

// Error case
return UserErrors.InvalidCredentials;

// Multiple errors
return new[] { UserErrors.InvalidEmail, UserErrors.DuplicateEmail };
```

**Benefits:**
- ✅ No exceptions for expected failures
- ✅ Type-safe error handling
- ✅ Forces error consideration
- ✅ Clear success/failure paths

### 2. Repository Pattern

Services depend on repository interfaces:

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    // Repository handles all data access
}
```

**Benefits:**
- ✅ Decouples from infrastructure
- ✅ Enables testing with mocks
- ✅ Clear separation of concerns

### 3. Interface Segregation

Separate interfaces for application and infrastructure concerns:

```
Interfaces/
├── Application/
│   └── IUserService.cs        # Service contract (used by controllers)
└── Infrastructure/
    └── IUserRepository.cs     # Repository contract (used by services)
```

---

## Error Handling

### Exception Handling Strategy

**Domain Exceptions:**
```csharp
try
{
    var user = new User(...);  // May throw ArgumentException
}
catch (ArgumentException ex)
{
    return UserErrors.ValidationError(ex.Message);
}
```

**Infrastructure Errors:**
```csharp
var result = await _userRepository.CreateAsync(user, ct);
if (result.IsError)
    return result.Errors;  // Repository returns ErrorOr
```

**Unexpected Errors:**
```csharp
catch (Exception ex)
{
    return UserErrors.RegistrationFailed(ex.Message);
}
```

### Error Response Flow

```
Controller receives request
    ↓
Service processes request
    ↓
Domain validates (throws ArgumentException)
    ↓
Service catches and converts to ErrorOr
    ↓
Controller receives ErrorOr
    ↓
Controller returns ProblemDetails
```

---

## Security Considerations

### Password Management

**Hashing:**
```csharp
var passwordHash = BCrypt.Net.BCrypt.HashPassword(
    password,
    BCrypt.Net.BCrypt.GenerateSalt()
);
```

**Verification:**
```csharp
if (!BCrypt.Net.BCrypt.Verify(plainPassword, storedHash))
{
    return UserErrors.InvalidCredentials;
}
```

**Best Practices:**
- ✅ BCrypt with automatic salt generation
- ✅ Never store plain passwords
- ✅ Never return passwords in responses
- ✅ Hash at application layer, not domain

### JWT Token Generation

```csharp
var token = _jwtTokenGenerator.GenerateToken(
    userId: user.Id,
    email: user.Email,
    name: user.Name
);
```

**Token Contains:**
- User ID
- Email
- Name
- Expiration timestamp

### Authorization Checks

```csharp
// Verify ownership before operations
if (userId != transaction.UserId)
    return TransactionErrors.Unauthorized;
```

### Security Principles

1. **Password Verification Always Required** for sensitive operations
2. **Same Error Messages** for "not found" vs "unauthorized" (don't leak info)
3. **Explicit Confirmation** for destructive operations
4. **Token Expiration** enforced in JWT
5. **No Sensitive Data** in responses or logs

---

## Testing Considerations

### Service Testing

Services should be tested with:
- ✅ Mock repositories
- ✅ Real domain entities
- ✅ ErrorOr assertions

**Example:**
```csharp
[Fact]
public async Task RegisterAsync_WithValidData_ShouldSucceed()
{
    // Arrange
    var mockRepository = new Mock<IUserRepository>();
    var service = new UserService(mockRepository.Object, ...);

    var request = new RegisterRequest(
        Name: "John Doe",
        Email: "john@example.com",
        Password: "Password123!",
        InitialBalance: 1000m
    );

    // Act
    var result = await service.RegisterAsync(request, CancellationToken.None);

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Name.Should().Be("John Doe");
}
```

### Key Test Scenarios

**User Service:**
- ✅ Register with valid data
- ✅ Register with duplicate email
- ✅ Login with correct credentials
- ✅ Login with wrong password
- ✅ Update profile with partial data
- ✅ Update with wrong current password
- ✅ Delete with confirmation
- ✅ Delete without confirmation

**Transaction Service:**
- ✅ Create transaction
- ✅ Update owned transaction
- ✅ Update unauthorized transaction
- ✅ Delete transaction
- ✅ Query with filters
- ✅ Query with pagination

---

## Best Practices

### 1. Keep Services Thin

Services should orchestrate, not implement logic:

❌ **Bad:**
```csharp
// Complex business logic in service
public async Task<ErrorOr<Result>> ComplexOperation(...)
{
    // 100 lines of business logic
}
```

✅ **Good:**
```csharp
// Domain entities handle logic, service orchestrates
public async Task<ErrorOr<Result>> ComplexOperation(...)
{
    var entity = new DomainEntity(...);  // Entity validates/calculates
    return await _repository.SaveAsync(entity);  // Service coordinates
}
```

### 2. Use Interfaces

Always program to interfaces:

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;  // Not concrete class
}
```

### 3. Handle All Error Paths

```csharp
var result = await _repository.GetAsync(id);
if (result.IsError)
    return result.Errors;  // Always check ErrorOr

var entity = result.Value;  // Safe to use
```

### 4. Validate Early

```csharp
if (userId <= 0)
    return UserErrors.InvalidUserId;  // Check before database call
```

### 5. Use CancellationTokens

```csharp
public async Task<ErrorOr<T>> OperationAsync(
    int id,
    CancellationToken cancellationToken)  // Always include
{
    return await _repository.GetAsync(id, cancellationToken);
}
```

---

## Additional Resources

### Related Documentation
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and layer responsibilities
- **[Domain Layer](../Domain/README.md)** - Business entities and validation rules
- **[Infrastructure Layer](../Infrastructure/README.md)** - Repository implementations
- **[Contracts Layer](../Contracts/README.md)** - Request/Response DTOs
- **[WebApi Layer](../WebApi/README.md)** - API endpoints

### Testing
- **[Application Tests](../../tests/Application.Tests/README.md)** - Tests for these services

---

## Summary

The Application layer orchestrates business operations:

- ✅ **Services** coordinate between layers
- ✅ **Security** handled here (hashing, tokens, authorization)
- ✅ **Validation** of cross-cutting concerns (uniqueness, complexity)
- ✅ **ErrorOr** for functional error handling
- ✅ **Interfaces** for loose coupling
- ✅ **Orchestration** without domain logic

**Core Principle:** The Application layer is the conductor of the orchestra - it coordinates the players (domain, infrastructure) but doesn't play the instruments itself.