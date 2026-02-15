# ExpenseTracker Contracts Layer

This document explains the Contracts layer of the ExpenseTracker API, which defines the API's public interface through request and response models (DTOs).

## Table of Contents

- [Overview](#overview)
- [Layer Purpose](#layer-purpose)
- [Contract Structure](#contract-structure)
- [User Contracts](#user-contracts)
- [Transaction Contracts](#transaction-contracts)
- [Transaction Group Contracts](#transaction-group-contracts)
- [Category Contracts](#category-contracts)
- [Validation Attributes](#validation-attributes)
- [Mapping Extensions](#mapping-extensions)

---

## Overview

The Contracts layer defines **Data Transfer Objects (DTOs)** that form the API's public contract. These objects:

- ✅ Define the structure of API requests and responses
- ✅ Include validation attributes for input validation
- ✅ Provide XML documentation for Swagger
- ✅ Separate API contracts from domain entities
- ✅ Enable API versioning without breaking domain logic

**Key Principle:** Contracts are the boundary between the external world and the internal domain. They translate between API representations and domain entities.

### Layer Structure

```
Contracts/
├── Users/
│   ├── RegisterRequest.cs
│   ├── RegisterResponse.cs
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   ├── UpdateUserRequest.cs
│   ├── UpdateUserResponse.cs
│   ├── DeleteUserRequest.cs
│   └── DeleteUserResponse.cs
├── Transactions/
│   ├── CreateTransactionRequest.cs
│   ├── UpdateTransactionRequest.cs
│   ├── TransactionResponse.cs
│   ├── TransactionFilter.cs
│   ├── TransactionFilterRequest.cs
│   ├── TransactionFilterResponse.cs
│   ├── TransactionRequestParser.cs
│   ├── TransactionFilterParser.cs
│   └── TransactionMappingExtensions.cs
├── TransactionGroups/
│   ├── CreateTransactionGroupRequest.cs
│   ├── UpdateTransactionGroupRequest.cs
│   ├── TransactionGroupResponse.cs
│   └── TransactionGroupMappingExtensions.cs
└── Categories/
    ├── CategoryResponse.cs
    └── CategoryMappingExtensions.cs
```

---

## Layer Purpose

### Why Separate Contracts from Domain?

**Without Contracts Layer:**
```csharp
// ❌ Exposing domain entity directly
[HttpPost]
public async Task<User> Create([FromBody] User user)  // Bad!
{
    return await _repository.CreateAsync(user);
}
```

**Problems:**
- ❌ Exposes internal domain structure
- ❌ Cannot evolve domain without breaking API
- ❌ Client controls domain object creation
- ❌ No API-specific validation
- ❌ Difficult to version

**With Contracts Layer:**
```csharp
// ✅ Using DTOs
[HttpPost]
public async Task<IActionResult> Create([FromBody] RegisterRequest request)  // Good!
{
    var result = await _service.RegisterAsync(request);
    return Ok(result.ToResponse());
}
```

**Benefits:**
- ✅ Clear API contract
- ✅ Domain can evolve independently
- ✅ API-specific validation
- ✅ Easy to version
- ✅ Type-safe mappings

---

## Contract Structure

### Request Contracts

Request contracts use **records** with validation attributes:

```csharp
public record RegisterRequest(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8)]
    string Password,

    decimal? InitialBalance = null
);
```

**Why Records?**
- ✅ Immutable by default
- ✅ Value-based equality
- ✅ Concise syntax
- ✅ Perfect for DTOs

### Response Contracts

Response contracts are **records** without validation:

```csharp
public record RegisterResponse(
    int Id,
    string Name,
    string Email,
    decimal InitialBalance,
    DateTime CreatedAt
);
```

**Response Design Principles:**
- ✅ Read-only (records are immutable)
- ✅ No validation needed (data already validated)
- ✅ Include only what the client needs
- ✅ Never expose sensitive data (e.g., password hashes)

---

## User Contracts

### RegisterRequest

**Purpose:** Create a new user account.

**Properties:**

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `Name` | string | Yes | Max 100 chars | User's full name |
| `Email` | string | Yes | Valid email, max 254 chars | Unique email address |
| `Password` | string | Yes | 8-100 chars, complexity rules | Plain password (hashed by service) |
| `InitialBalance` | decimal? | No | Any value | Starting account balance (default: 0) |

**Password Validation:**
```regex
^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$
```
- At least one lowercase letter
- At least one uppercase letter
- At least one digit
- At least one special character
- Minimum 8 characters

**Example:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "Password123!",
  "initialBalance": 1000.00
}
```

---

### RegisterResponse

**Purpose:** Return newly created user details.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | Unique user identifier |
| `Name` | string | User's full name |
| `Email` | string | User's email address |
| `InitialBalance` | decimal | Starting balance |
| `CreatedAt` | DateTime | Account creation timestamp (UTC) |

**Example:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "initialBalance": 1000.00,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

---

### LoginRequest

**Purpose:** Authenticate user and obtain JWT token.

**Properties:**

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Email` | string | Yes | User's email address |
| `Password` | string | Yes | User's plain password |

**Example:**
```json
{
  "email": "john.doe@example.com",
  "password": "Password123!"
}
```

---

### LoginResponse

**Purpose:** Return authenticated user details with JWT token.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | User identifier |
| `Name` | string | User's full name |
| `Email` | string | User's email address |
| `InitialBalance` | decimal | User's balance |
| `Token` | string | JWT access token |
| `ExpiresAt` | DateTime | Token expiration timestamp (UTC) |

**Example:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "initialBalance": 1000.00,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-16T10:30:00Z"
}
```

---

### UpdateUserRequest

**Purpose:** Update user profile (partial update pattern).

**Properties:**

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `Name` | string? | No | Max 100 chars | New name (only if provided) |
| `Email` | string? | No | Valid email, max 254 chars | New email (only if provided) |
| `NewPassword` | string? | No | 8-100 chars, complexity rules | New password (only if provided) |
| `CurrentPassword` | string | **Yes** | Max 100 chars | Required for security verification |
| `InitialBalance` | decimal? | No | Any value | New balance (only if provided) |

**Partial Update Design:**
- Only `CurrentPassword` is required
- All other fields are optional
- Null fields are ignored (not updated)
- Allows updating individual fields without sending all data

**Example (update only name):**
```json
{
  "name": "John Smith",
  "currentPassword": "Password123!"
}
```

**Example (update multiple fields):**
```json
{
  "name": "John Smith",
  "email": "john.smith@example.com",
  "newPassword": "NewPassword456!",
  "currentPassword": "Password123!",
  "initialBalance": 1500.00
}
```

---

### UpdateUserResponse

**Purpose:** Return updated user details.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | User identifier |
| `Name` | string | Updated name |
| `Email` | string | Updated email |
| `InitialBalance` | decimal | Updated balance |
| `UpdatedAt` | DateTime | Last update timestamp (UTC) |

---

### DeleteUserRequest

**Purpose:** Permanently delete user account with confirmation.

**Properties:**

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `CurrentPassword` | string | Yes | Password verification |
| `ConfirmDeletion` | bool | Yes | Must be `true` to confirm intent |

**Example:**
```json
{
  "currentPassword": "Password123!",
  "confirmDeletion": true
}
```

---

### DeleteUserResponse

**Purpose:** Confirm account deletion.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | Deleted user identifier |
| `Name` | string | Deleted user name |
| `Email` | string | Deleted user email |
| `DeletedAt` | DateTime | Deletion timestamp (UTC) |
| `Message` | string | Confirmation message |

---

## Transaction Contracts

### CreateTransactionRequest

**Purpose:** Create a new transaction (expense or income).

**Properties:**

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `TransactionType` | string | Yes | "EXPENSE" or "INCOME" | Transaction type |
| `Amount` | decimal | Yes | > 0.01 | Absolute amount |
| `Date` | DateOnly | Yes | Valid date | Transaction date |
| `Subject` | string | Yes | 1-255 chars | Brief description |
| `Notes` | string? | No | Max 2000 chars | Detailed notes |
| `PaymentMethod` | string | Yes | Valid enum value | Payment method |
| `CategoryId` | int? | No | > 0 if provided | Category assignment |
| `TransactionGroupId` | int? | No | > 0 if provided | Group assignment |

**Valid Transaction Types:**
- `EXPENSE` - Money going out
- `INCOME` - Money coming in

**Valid Payment Methods:**
- `CASH`
- `DEBIT_CARD`
- `CREDIT_CARD`
- `BANK_TRANSFER`
- `MOBILE_PAYMENT`
- `PAYPAL`
- `CRYPTO`
- `OTHER`

**Example:**
```json
{
  "transactionType": "EXPENSE",
  "amount": 45.50,
  "date": "2024-01-15",
  "subject": "Grocery shopping",
  "notes": "Weekly groceries from Whole Foods",
  "paymentMethod": "DEBIT_CARD",
  "categoryId": 1,
  "transactionGroupId": 2
}
```

---

### UpdateTransactionRequest

**Purpose:** Update an existing transaction.

**Properties:** Same as `CreateTransactionRequest`.

**Note:** This is a full update, not partial. All fields must be provided.

---

### TransactionResponse

**Purpose:** Return transaction details with calculated fields.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | Transaction identifier |
| `UserId` | int | Owner user ID |
| `TransactionType` | string | "EXPENSE" or "INCOME" |
| `Amount` | decimal | Absolute amount (always positive) |
| `SignedAmount` | decimal | Signed amount (negative for expense) |
| `Date` | DateOnly | Transaction date |
| `Subject` | string | Brief description |
| `Notes` | string? | Detailed notes |
| `PaymentMethod` | string | Payment method |
| `CumulativeDelta` | decimal | Running balance at this transaction |
| `CategoryId` | int? | Category ID (null if uncategorized) |
| `CategoryName` | string? | Category name |
| `TransactionGroupId` | int? | Group ID (null if ungrouped) |
| `TransactionGroupName` | string? | Group name |
| `CreatedAt` | DateTime | Creation timestamp (UTC) |
| `UpdatedAt` | DateTime | Last update timestamp (UTC) |

**Key Fields:**

- **SignedAmount**: Automatically calculated
  - Expense: `-Amount` (e.g., -45.50)
  - Income: `+Amount` (e.g., +500.00)

- **CumulativeDelta**: Running balance
  - Sum of all signed amounts up to this transaction
  - User's actual balance = `InitialBalance + CumulativeDelta`

**Example:**
```json
{
  "id": 1,
  "userId": 1,
  "transactionType": "EXPENSE",
  "amount": 45.50,
  "signedAmount": -45.50,
  "date": "2024-01-15",
  "subject": "Grocery shopping",
  "notes": "Weekly groceries",
  "paymentMethod": "DEBIT_CARD",
  "cumulativeDelta": 450.00,
  "categoryId": 1,
  "categoryName": "Food & Dining",
  "transactionGroupId": 2,
  "transactionGroupName": "January Budget",
  "createdAt": "2024-01-15T14:30:00Z",
  "updatedAt": "2024-01-15T14:30:00Z"
}
```

---

### TransactionFilter

**Purpose:** Internal validated filter model for querying transactions.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `TransactionType` | TransactionType? | Filter by type |
| `MinAmount` | decimal? | Minimum amount (inclusive) |
| `MaxAmount` | decimal? | Maximum amount (inclusive) |
| `DateFrom` | DateOnly? | Start date (inclusive) |
| `DateTo` | DateOnly? | End date (inclusive) |
| `SubjectContains` | string? | Search in subject (case-insensitive) |
| `NotesContains` | string? | Search in notes (case-insensitive) |
| `PaymentMethods` | IReadOnlyList<PaymentMethod>? | Filter by payment methods |
| `CategoryIds` | IReadOnlyList<int>? | Filter by categories |
| `Uncategorized` | bool | Only uncategorized transactions |
| `TransactionGroupIds` | IReadOnlyList<int>? | Filter by groups |
| `Ungrouped` | bool | Only ungrouped transactions |
| `SortBy` | TransactionSortField | Field to sort by |
| `SortDescending` | bool | Sort direction (default: true) |
| `Page` | int | Page number (1-based) |
| `PageSize` | int | Items per page |

**Sort Fields:**
- `Date` - By transaction date (default)
- `Amount` - By absolute amount
- `Subject` - Alphabetically
- `PaymentMethod` - By payment method
- `CreatedAt` - By creation time
- `UpdatedAt` - By last update time

**Helper Properties:**
- `Skip` - Calculates items to skip for pagination
- `HasFilters` - Returns true if any filter is active

---

### TransactionFilterResponse

**Purpose:** Return paginated transaction results.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Transactions` | List<TransactionResponse> | Matching transactions |
| `TotalCount` | int | Total matching items (all pages) |
| `Page` | int | Current page number |
| `PageSize` | int | Items per page |
| `TotalPages` | int | Total number of pages |

**Example:**
```json
{
  "transactions": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

## Transaction Group Contracts

### CreateTransactionGroupRequest

**Purpose:** Create a new transaction group.

**Properties:**

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `Name` | string | Yes | Max 100 chars | Group name |
| `Description` | string? | No | Max 500 chars | Group description |

**Example:**
```json
{
  "name": "Vacation Trip 2024",
  "description": "Summer vacation expenses to Europe"
}
```

---

### UpdateTransactionGroupRequest

**Purpose:** Update an existing transaction group.

**Properties:** Same as `CreateTransactionGroupRequest`.

---

### TransactionGroupResponse

**Purpose:** Return transaction group details.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | Group identifier |
| `Name` | string | Group name |
| `Description` | string? | Group description |
| `UserId` | int | Owner user ID |
| `CreatedAt` | DateTime | Creation timestamp (UTC) |

**Example:**
```json
{
  "id": 1,
  "name": "Vacation Trip 2024",
  "description": "Summer vacation expenses to Europe",
  "userId": 1,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

---

## Category Contracts

### CategoryResponse

**Purpose:** Return category details.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | Category identifier |
| `Name` | string | Category name |
| `Description` | string? | Category description |
| `Icon` | string | Icon identifier for UI |

**Example:**
```json
{
  "id": 1,
  "name": "Food & Dining",
  "description": "Groceries, restaurants, cafes",
  "icon": "utensils"
}
```

---

## Validation Attributes

### Common Attributes

**`[Required]`**
```csharp
[Required(ErrorMessage = "Name is required")]
string Name
```

**`[StringLength]`**
```csharp
[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
string Password
```

**`[EmailAddress]`**
```csharp
[EmailAddress(ErrorMessage = "Email format is invalid")]
string Email
```

**`[Range]`**
```csharp
[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
decimal Amount
```

**`[RegularExpression]`**
```csharp
[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
    ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character")]
string Password
```

---

## Mapping Extensions

Mapping extensions convert between domain entities and DTOs.

### User Mapping

```csharp
public static RegisterResponse ToRegisterResponse(this User user)
{
    return new RegisterResponse(
        Id: user.Id,
        Name: user.Name,
        Email: user.Email,
        InitialBalance: user.InitialBalance,
        CreatedAt: user.CreatedAt
    );
}
```

### Transaction Mapping

```csharp
public static TransactionResponse ToResponse(this Transaction transaction)
{
    return new TransactionResponse(
        Id: transaction.Id,
        UserId: transaction.UserId,
        TransactionType: transaction.TransactionType.ToString(),
        Amount: transaction.Amount,
        SignedAmount: transaction.SignedAmount,
        Date: transaction.Date,
        Subject: transaction.Subject,
        Notes: transaction.Notes,
        PaymentMethod: transaction.PaymentMethod.ToString(),
        CumulativeDelta: transaction.CumulativeDelta,
        // ... other fields
    );
}
```

---

## Best Practices

### 1. Never Expose Domain Entities

❌ **Bad:**
```csharp
[HttpGet]
public async Task<User> GetUser(int id)
{
    return await _repository.GetByIdAsync(id);
}
```

✅ **Good:**
```csharp
[HttpGet]
public async Task<IActionResult> GetUser(int id)
{
    var user = await _repository.GetByIdAsync(id);
    return Ok(user.ToResponse());
}
```

### 2. Use Records for DTOs

✅ Records provide:
- Immutability
- Value equality
- Concise syntax
- Perfect for DTOs

### 3. Include XML Documentation

```csharp
/// <summary>
/// Request contract for user registration.
/// </summary>
/// <param name="Name">User's full name (required, max 100 characters)</param>
public record RegisterRequest(string Name, ...);
```

### 4. Validate at the Boundary

- ✅ Use data annotations on contracts
- ✅ Validate before passing to domain
- ✅ Return clear validation errors

### 5. Separate Request and Response

- ✅ Different models for input/output
- ✅ Request: includes validation
- ✅ Response: includes computed fields

### 6. Use Meaningful Names

- ✅ `RegisterRequest`, not `UserInput`
- ✅ `TransactionResponse`, not `TransactionDto`
- ✅ Clear intent from name alone

---

## Additional Resources

### Related Documentation
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and layer responsibilities
- **[Domain Layer](../ExpenseTrackerAPI.Domain/README.md)** - Business entities and validation rules
- **[Application Layer](../ExpenseTrackerAPI.Application/README.md)** - Services using these contracts
- **[Infrastructure Layer](../ExpenseTrackerAPI.Infrastructure/README.md)** - Repository implementations
- **[WebApi Layer](../ExpenseTrackerAPI.WebApi/README.md)** - Controllers using these contracts

### Testing
- **[Domain Tests](../../tests/ExpenseTrackerAPI.Domain.Tests/README.md)** - Entity validation tests
- **[WebApi Tests](../../tests/ExpenseTrackerAPI.WebApi.Tests/README.md)** - E2E tests using these contracts

---

## Summary

The Contracts layer defines the API's public interface:

- ✅ **DTOs** separate API from domain
- ✅ **Validation** happens at the boundary
- ✅ **Records** provide immutable DTOs
- ✅ **Mapping** converts between layers
- ✅ **Versioning** enabled without breaking domain

**Core Principle:** Contracts are the API's promise to clients. Keep them stable, well-documented, and separate from internal implementation details.