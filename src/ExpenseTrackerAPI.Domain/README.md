# ExpenseTracker Domain Layer

This document explains the Domain layer of the ExpenseTracker API, focusing on the business validation rules for **Transactions** and **Users**.

## Table of Contents

- [Overview](#overview)
- [Domain Entities](#domain-entities)
- [User Validation Rules](#user-validation-rules)
- [Transaction Validation Rules](#transaction-validation-rules)
- [Domain Errors](#domain-errors)
- [Validation Philosophy](#validation-philosophy)

---

## Overview

The Domain layer contains the core business entities and validation rules. It enforces business logic constraints at the entity level, ensuring data integrity from the ground up.

**Key Principles:**
- ✅ **Validation at construction** - Entities cannot be created in an invalid state
- ✅ **Immutability where possible** - Most properties are read-only after construction
- ✅ **Business rule enforcement** - Domain entities guard their invariants
- ✅ **No infrastructure dependencies** - Pure domain logic

### Layer Structure

```
Domain/
├── Entities/
│   ├── User.cs                    # User entity with validation
│   ├── Transaction.cs             # Transaction entity with validation
│   ├── Category.cs
│   ├── TransactionGroup.cs
│   ├── TransactionType.cs         # Enum: EXPENSE, INCOME
│   └── PaymentMethod.cs           # Enum: CASH, DEBIT_CARD, etc.
├── Errors/
│   ├── UserErrors.cs              # Domain errors for users
│   ├── TransactionErrors.cs       # Domain errors for transactions
│   ├── CategoryErrors.cs
│   └── TransactionGroupErrors.cs
└── Constants/
    └── (Domain constants)
```

---

## Domain Entities

### Entity Design Pattern

All domain entities follow this pattern:

```csharp
public class Entity
{
    // Private parameterless constructor for EF Core
    private Entity() { }

    // Public constructor with validation
    public Entity(params)
    {
        ValidateBusinessRules(params);
        // Set properties
    }

    // Business rule validation method
    private static void ValidateBusinessRules(params)
    {
        // Throw ArgumentException on validation failure
    }

    // Read-only properties
    public int Id { get; private set; }
    public string Property { get; }
}
```

**Why this design?**
- ✅ Entities cannot be constructed in invalid state
- ✅ EF Core can still materialize entities from database
- ✅ Clear separation between creation and persistence
- ✅ Validation failures throw exceptions immediately

---

## User Validation Rules

### User Entity

The `User` entity represents a user account in the expense tracking system.

**Properties:**
- `Id` - Unique identifier (auto-generated)
- `Name` - User's display name
- `Email` - User's email address (unique)
- `PasswordHash` - Hashed password (never plain text)
- `InitialBalance` - Starting account balance
- `CreatedAt` - Account creation timestamp
- `UpdatedAt` - Last update timestamp

### 1. Name Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Cannot be null, empty, or whitespace | "Name cannot be empty" |
| **Max Length** | 100 characters | "Name cannot exceed 100 characters" |
| **Trimming** | Automatically trimmed before validation | - |

**Example Valid Names:**
```csharp
✅ "John Doe"
✅ "Mary Jane Smith"
✅ "李明" (Unicode supported)
✅ "O'Connor" (Special characters allowed)
```

**Example Invalid Names:**
```csharp
❌ ""                    // Empty
❌ "   "                 // Whitespace only
❌ null                  // Null
❌ "A very long name..." // > 100 characters
```

**Code Implementation:**
```csharp
// Name validation
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Name cannot be empty", nameof(name));

if (name.Trim().Length > 100)
    throw new ArgumentException("Name cannot exceed 100 characters", nameof(name));
```

---

### 2. Email Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Cannot be null, empty, or whitespace | "Email cannot be empty" |
| **Max Length** | 254 characters (RFC 5321 standard) | "Email cannot exceed 254 characters" |
| **Format** | Must match email regex pattern | "Email format is invalid" |
| **No Consecutive Dots** | Cannot contain ".." | "Email format is invalid" |
| **Normalization** | Converted to lowercase | - |

**Email Regex Pattern:**
```regex
^[a-zA-Z0-9]([a-zA-Z0-9._%+-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?)*\.[a-zA-Z]{2,}$
```

**Pattern Requirements:**
- Must start and end with alphanumeric characters (no leading/trailing dots or special chars)
- Local part (before @) can contain: letters, digits, dots, underscores, percent, plus, hyphen
- Domain part must have valid structure
- Must end with valid TLD (2+ letters)

**Example Valid Emails:**
```csharp
✅ "user@example.com"
✅ "john.doe@company.co.uk"
✅ "test+filter@gmail.com"
✅ "user_123@test-domain.io"
✅ "UPPERCASE@EXAMPLE.COM"  // Normalized to lowercase
```

**Example Invalid Emails:**
```csharp
❌ "invalid"                        // No @ symbol
❌ "@example.com"                   // Missing local part
❌ "user@"                          // Missing domain
❌ "user..name@example.com"         // Consecutive dots
❌ ".user@example.com"              // Leading dot
❌ "user.@example.com"              // Trailing dot before @
❌ "user@example"                   // Missing TLD
❌ "user@.example.com"              // Domain starts with dot
❌ "user@example.c"                 // TLD too short
```

**Code Implementation:**
```csharp
// Email validation
if (string.IsNullOrWhiteSpace(email))
    throw new ArgumentException("Email cannot be empty", nameof(email));

if (email.Length > 254) // RFC 5321 limit
    throw new ArgumentException("Email cannot exceed 254 characters", nameof(email));

if (!EmailRegex().IsMatch(email) || email.Contains(".."))
    throw new ArgumentException("Email format is invalid", nameof(email));

// EmailRegex is a compiled regex using [GeneratedRegex] attribute
```

---

### 3. Password Validation

**Note:** The Domain layer only validates that the password **hash** is not empty. Password complexity validation happens at the **Application layer** before hashing.

**Domain Rules (Password Hash):**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Cannot be null, empty, or whitespace | "Password hash cannot be empty" |

**Application Layer Rules (Plain Password):**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Min Length** | 8 characters | "Password must be at least 8 characters long" |
| **Max Length** | 100 characters | "Password cannot exceed 100 characters" |
| **Uppercase** | At least one uppercase letter (A-Z) | "Password must contain at least one uppercase letter" |
| **Lowercase** | At least one lowercase letter (a-z) | "Password must contain at least one lowercase letter" |
| **Digit** | At least one digit (0-9) | "Password must contain at least one digit" |
| **Special Character** | At least one non-alphanumeric character | "Password must contain at least one special character" |

**Example Valid Passwords:**
```csharp
✅ "Password123!"
✅ "MySecure@Pass1"
✅ "C0mpl3x!Pass"
✅ "Test@1234"
```

**Example Invalid Passwords:**
```csharp
❌ "short1!"              // Less than 8 characters
❌ "password123!"         // No uppercase letter
❌ "PASSWORD123!"         // No lowercase letter
❌ "PasswordTest!"        // No digit
❌ "Password123"          // No special character
❌ "simple"               // Too short, no complexity
```

**Code Implementation (Application Layer):**
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

### 4. Initial Balance Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Optional** | Can be any decimal value (positive, negative, zero) | - |
| **Default** | 0 if not provided | - |
| **Precision** | Stored as DECIMAL(12,2) in database | - |

**Example Valid Initial Balances:**
```csharp
✅ 0m          // Default
✅ 1000.00m    // Positive balance
✅ -500.50m    // Negative balance (debt)
✅ 999999.99m  // Large balance
```

**No restrictions** - Users can start with any balance (including negative for debt tracking).

---

### User Validation Summary

**Constructor Validation Flow:**
```
User Constructor
    ↓
Normalize inputs (trim, lowercase email)
    ↓
ValidateBusinessRules()
    ├─→ Name: not empty, ≤ 100 chars
    ├─→ Email: not empty, ≤ 254 chars, valid format, no ".."
    └─→ PasswordHash: not empty
    ↓
Set properties
    ↓
User created ✓
```

---

## Transaction Validation Rules

### Transaction Entity

The `Transaction` entity represents a financial transaction (expense or income).

**Properties:**
- `Id` - Unique identifier (auto-generated)
- `UserId` - Owner of the transaction
- `TransactionType` - EXPENSE or INCOME
- `Amount` - Absolute amount (always positive)
- `SignedAmount` - Signed amount (negative for expense, positive for income)
- `Date` - Transaction date (DateOnly, no time)
- `Subject` - Brief description
- `Notes` - Optional detailed notes
- `PaymentMethod` - Payment method enum
- `CumulativeDelta` - Running balance at this transaction
- `CategoryId` - Optional category
- `TransactionGroupId` - Optional group
- `CreatedAt` - Creation timestamp
- `UpdatedAt` - Last update timestamp

---

### 1. User ID Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Must be provided | - |
| **Positive** | Must be > 0 | "User ID must be a positive integer." |

**Example:**
```csharp
✅ userId = 1
✅ userId = 42
❌ userId = 0
❌ userId = -1
```

**Code Implementation:**
```csharp
if (userId <= 0)
{
    throw new ArgumentException("User ID must be a positive integer.", nameof(userId));
}
```

---

### 2. Transaction Type Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Must be provided | - |
| **Valid Enum** | Must be EXPENSE or INCOME | "Invalid transaction type. Must be 'EXPENSE' or 'INCOME'." |

**Valid Transaction Types:**
```csharp
public enum TransactionType
{
    EXPENSE,  // Money going out (reduces balance)
    INCOME    // Money coming in (increases balance)
}
```

**Example:**
```csharp
✅ TransactionType.EXPENSE
✅ TransactionType.INCOME
❌ (TransactionType)999  // Invalid enum value
```

**Code Implementation:**
```csharp
if (transactionType != TransactionType.EXPENSE && transactionType != TransactionType.INCOME)
{
    throw new ArgumentException("Invalid transaction type. Must be 'EXPENSE' or 'INCOME'.", nameof(transactionType));
}
```

---

### 3. Amount Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Must be provided | - |
| **Positive** | Must be > 0 | "Transaction amount must be greater than zero." |
| **Precision** | Stored as DECIMAL(12,2) | - |

**Example Valid Amounts:**
```csharp
✅ 0.01m       // Minimum (1 cent)
✅ 10.50m      // Typical
✅ 1000.00m    // Large
✅ 999999.99m  // Maximum practical
```

**Example Invalid Amounts:**
```csharp
❌ 0m          // Zero not allowed
❌ -50.00m     // Negative not allowed (use TransactionType instead)
```

**Important:** The `Amount` property is **always positive**. The transaction type (EXPENSE or INCOME) determines the sign:
- **Expense**: `SignedAmount = -Amount` (e.g., -50.00)
- **Income**: `SignedAmount = +Amount` (e.g., +50.00)

**Code Implementation:**
```csharp
if (amount <= 0)
{
    throw new ArgumentException("Transaction amount must be greater than zero.", nameof(amount));
}

// Automatic signed amount calculation
SignedAmount = transactionType == TransactionType.EXPENSE ? -amount : amount;
```

---

### 4. Date Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Must be provided | - |
| **Min Date** | 1900-01-01 | "Transaction date must be between 1900-01-01 and {maxDate}." |
| **Max Date** | Current date + 1 year | "Transaction date must be between {minDate} and {maxDate}." |
| **Type** | DateOnly (no time component) | - |

**Date Range Logic:**
```csharp
var minDate = new DateOnly(1900, 1, 1);
var maxDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1));
```

**Example Valid Dates:**
```csharp
✅ new DateOnly(2024, 1, 15)   // Current date
✅ new DateOnly(2023, 6, 1)    // Past date
✅ new DateOnly(2025, 12, 31)  // Future date (within 1 year)
✅ new DateOnly(1900, 1, 1)    // Minimum date
```

**Example Invalid Dates:**
```csharp
❌ new DateOnly(1899, 12, 31)  // Before 1900
❌ new DateOnly(2026, 6, 1)    // More than 1 year in future
```

**Why allow future dates?**
- Supports scheduled/recurring transactions
- Allows planning future expenses
- Limited to 1 year to prevent data entry errors

**Code Implementation:**
```csharp
var minDate = new DateOnly(1900, 1, 1);
var maxDate = DateOnly.FromDateTime(DateTime.Now.AddYears(1));

if (date < minDate || date > maxDate)
{
    throw new ArgumentException(
        $"Transaction date must be between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}.", 
        nameof(date));
}
```

---

### 5. Subject Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Cannot be null, empty, or whitespace | "Transaction subject is required and cannot be empty." |
| **Max Length** | 255 characters | "Transaction subject cannot exceed 255 characters." |
| **Trimming** | Automatically trimmed before validation | - |

**Example Valid Subjects:**
```csharp
✅ "Grocery shopping"
✅ "Monthly salary"
✅ "Flight tickets to NYC"
✅ "Coffee at Starbucks"
✅ "Birthday gift for Mom"
```

**Example Invalid Subjects:**
```csharp
❌ ""                          // Empty
❌ "   "                       // Whitespace only
❌ null                        // Null
❌ "A very long description..." // > 255 characters
```

**Code Implementation:**
```csharp
if (string.IsNullOrWhiteSpace(subject))
{
    throw new ArgumentException("Transaction subject is required and cannot be empty.", nameof(subject));
}

if (subject.Trim().Length > 255)
{
    throw new ArgumentException("Transaction subject cannot exceed 255 characters.", nameof(subject));
}

Subject = subject.Trim();
```

---

### 6. Notes Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Optional** | Can be null or empty | - |
| **Max Length** | Unlimited (stored as TEXT in database) | - |
| **Trimming** | Automatically trimmed if provided | - |
| **Null Conversion** | Empty/whitespace converted to null | - |

**Example:**
```csharp
✅ null                                    // No notes
✅ ""                                      // Converted to null
✅ "Additional details about transaction"  // Valid notes
✅ "Very long detailed notes..."           // No length limit
```

**Code Implementation:**
```csharp
Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
```

---

### 7. Payment Method Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Required** | Must be provided | - |
| **Valid Enum** | Must be a valid PaymentMethod enum value | - |

**Valid Payment Methods:**
```csharp
public enum PaymentMethod
{
    CASH,
    DEBIT_CARD,
    CREDIT_CARD,
    BANK_TRANSFER,
    MOBILE_PAYMENT,
    PAYPAL,
    CRYPTO,
    OTHER
}
```

**Example:**
```csharp
✅ PaymentMethod.CASH
✅ PaymentMethod.CREDIT_CARD
✅ PaymentMethod.PAYPAL
❌ (PaymentMethod)999  // Invalid enum value
```

**Note:** .NET enum validation happens at deserialization level. Invalid enum values are caught before reaching the domain layer.

---

### 8. Category ID Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Optional** | Can be null | - |
| **Positive if provided** | If provided, must be > 0 | "Category ID must be a positive integer when provided." |

**Example:**
```csharp
✅ null           // No category (uncategorized)
✅ 1              // Valid category ID
✅ 42             // Valid category ID
❌ 0              // Invalid (if provided)
❌ -1             // Invalid (if provided)
```

**Code Implementation:**
```csharp
if (categoryId.HasValue && categoryId.Value <= 0)
{
    throw new ArgumentException("Category ID must be a positive integer when provided.", nameof(categoryId));
}
```

---

### 9. Transaction Group ID Validation

**Rules:**

| Rule | Value | Error Message |
|------|-------|---------------|
| **Optional** | Can be null | - |
| **No validation** | Any integer value accepted if provided | - |

**Example:**
```csharp
✅ null           // No group (ungrouped)
✅ 1              // Valid group ID
✅ 42             // Valid group ID
```

**Note:** Foreign key constraints at the database level ensure the group exists.

---

### Transaction Validation Summary

**Constructor Validation Flow:**
```
Transaction Constructor
    ↓
ValidateBusinessRules()
    ├─→ UserId: > 0
    ├─→ TransactionType: EXPENSE or INCOME
    ├─→ Amount: > 0
    ├─→ Date: between 1900-01-01 and now+1year
    ├─→ Subject: not empty, ≤ 255 chars (trimmed)
    └─→ CategoryId: > 0 if provided
    ↓
Calculate SignedAmount
    ├─→ EXPENSE: -amount
    └─→ INCOME: +amount
    ↓
Normalize Notes (trim or null)
    ↓
Set properties
    ↓
Transaction created ✓
```

---

## Domain Errors

Domain errors are defined using the `ErrorOr` library, providing strongly-typed error handling.

### User Errors

**Location:** `Domain/Errors/UserErrors.cs`

**Available Errors:**
```csharp
UserErrors.NotFound                    // User not found
UserErrors.DuplicateEmail              // Email already exists
UserErrors.InvalidEmail                // Email format invalid
UserErrors.InvalidName                 // Name validation failed
UserErrors.InvalidPassword             // Password validation failed
UserErrors.WeakPassword                // Password complexity insufficient
UserErrors.EmailRequired               // Email is required
UserErrors.NameRequired                // Name is required
UserErrors.PasswordRequired            // Password is required
UserErrors.InvalidCredentials          // Login failed
UserErrors.InvalidUserId               // User ID validation failed
UserErrors.ConcurrencyError            // Concurrent modification
UserErrors.DeletionConfirmation        // Deletion not confirmed
```

**Example Usage:**
```csharp
if (user == null)
{
    return UserErrors.NotFound;
}

if (existingUser != null)
{
    return UserErrors.DuplicateEmail;
}
```

---

### Transaction Errors

**Location:** `Domain/Errors/TransactionErrors.cs`

**Available Errors:**
```csharp
TransactionErrors.NotFound                  // Transaction not found
TransactionErrors.InvalidTransactionId      // Transaction ID invalid
TransactionErrors.InvalidAmount             // Amount validation failed
TransactionErrors.InvalidTransactionType    // Invalid type
TransactionErrors.TransactionTypeRequired   // Type is required
TransactionErrors.InvalidPaymentMethod      // Invalid payment method
TransactionErrors.PaymentMethodRequired     // Payment method required
TransactionErrors.InvalidSubject            // Subject validation failed
TransactionErrors.InvalidUserId             // User ID invalid
TransactionErrors.InvalidDate               // Date out of range
TransactionErrors.SubjectTooLong            // Subject exceeds 255 chars
TransactionErrors.InvalidCategoryId         // Category ID invalid
TransactionErrors.Unauthorized              // Not authorized
TransactionErrors.ConcurrencyConflict       // Concurrent modification
TransactionErrors.InvalidPageNumber         // Pagination error
TransactionErrors.InvalidPageSize           // Pagination error
```

**Example Usage:**
```csharp
if (transaction == null)
{
    return TransactionErrors.NotFound;
}

if (transaction.UserId != currentUserId)
{
    return TransactionErrors.Unauthorized;
}
```

---

## Validation Philosophy

### 1. Fail Fast

Validation happens **at entity construction** - invalid entities cannot be created.

```csharp
// ✅ This throws immediately if invalid
var user = new User(
    name: "",  // ← ArgumentException thrown here
    email: "test@example.com",
    passwordHash: "hashed_password"
);
```

### 2. Layer Responsibility

**Domain Layer (this layer):**
- ✅ Business rule validation (required fields, formats, ranges)
- ✅ Entity invariants (things that must always be true)
- ✅ Data type validation

**Application Layer:**
- ✅ Password complexity validation (before hashing)
- ✅ Duplicate email checking (requires database)
- ✅ Authorization checks
- ✅ Business workflow validation

**Presentation Layer (WebApi/Contracts):**
- ✅ Data annotations for API contract validation
- ✅ Model binding validation
- ✅ Request format validation

### 3. Clear Error Messages

All validation errors include:
- **What** went wrong
- **Which** field failed
- **Why** it failed

```csharp
throw new ArgumentException(
    "Transaction date must be between 1900-01-01 and 2025-12-31.",
    nameof(date)
);
```

### 4. Normalization

Data is normalized before validation:
- **Trimming** - Remove leading/trailing whitespace
- **Email lowercase** - `test@EXAMPLE.com` → `test@example.com`
- **Null conversion** - Empty strings become null where appropriate

```csharp
var normalizedName = name?.Trim() ?? string.Empty;
var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
```

### 5. Immutability

Most properties are **read-only after construction**:
```csharp
public int UserId { get; }              // Cannot change
public decimal Amount { get; }          // Cannot change
public DateOnly Date { get; }           // Cannot change
```

**Why?**
- ✅ Prevents accidental modification
- ✅ Thread-safe reads
- ✅ Clear intent - create new entity to change values

**Exceptions:**
- `Id` - Set by database after insert
- `CumulativeDelta` - Updated by infrastructure layer
- `UpdatedAt` - Updated by entity methods

---

## Validation Examples

### Complete User Validation Example

```csharp
// ✅ Valid user creation
var user = new User(
    name: "John Doe",
    email: "john.doe@example.com",
    passwordHash: BCrypt.HashPassword("Password123!"),
    initialBalance: 1000.00m
);

// ❌ Invalid: empty name
var user = new User(
    name: "",  // ← ArgumentException: "Name cannot be empty"
    email: "john@example.com",
    passwordHash: "hashed",
    initialBalance: 0
);

// ❌ Invalid: bad email format
var user = new User(
    name: "John Doe",
    email: "not-an-email",  // ← ArgumentException: "Email format is invalid"
    passwordHash: "hashed",
    initialBalance: 0
);

// ❌ Invalid: name too long
var user = new User(
    name: new string('A', 101),  // ← ArgumentException: "Name cannot exceed 100 characters"
    email: "john@example.com",
    passwordHash: "hashed",
    initialBalance: 0
);
```

### Complete Transaction Validation Example

```csharp
// ✅ Valid transaction creation
var transaction = new Transaction(
    userId: 1,
    transactionType: TransactionType.EXPENSE,
    amount: 50.00m,
    date: new DateOnly(2024, 1, 15),
    subject: "Grocery shopping",
    paymentMethod: PaymentMethod.DEBIT_CARD,
    notes: "Weekly groceries from Whole Foods",
    categoryId: 1,
    transactionGroupId: 2
);

// ❌ Invalid: zero amount
var transaction = new Transaction(
    userId: 1,
    transactionType: TransactionType.EXPENSE,
    amount: 0m,  // ← ArgumentException: "Transaction amount must be greater than zero"
    date: new DateOnly(2024, 1, 15),
    subject: "Test",
    paymentMethod: PaymentMethod.CASH
);

// ❌ Invalid: empty subject
var transaction = new Transaction(
    userId: 1,
    transactionType: TransactionType.EXPENSE,
    amount: 50.00m,
    date: new DateOnly(2024, 1, 15),
    subject: "",  // ← ArgumentException: "Transaction subject is required and cannot be empty"
    paymentMethod: PaymentMethod.CASH
);

// ❌ Invalid: date too far in future
var transaction = new Transaction(
    userId: 1,
    transactionType: TransactionType.EXPENSE,
    amount: 50.00m,
    date: new DateOnly(2026, 1, 15),  // ← ArgumentException: "Transaction date must be between..."
    subject: "Test",
    paymentMethod: PaymentMethod.CASH
);

// ❌ Invalid: invalid category ID
var transaction = new Transaction(
    userId: 1,
    transactionType: TransactionType.EXPENSE,
    amount: 50.00m,
    date: new DateOnly(2024, 1, 15),
    subject: "Test",
    paymentMethod: PaymentMethod.CASH,
    categoryId: 0  // ← ArgumentException: "Category ID must be a positive integer when provided"
);
```

---

## Additional Resources

### Related Documentation
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and layer responsibilities
- **[Application Layer](../ExpenseTrackerAPI.Application/README.md)** - Services using these entities
- **[Infrastructure Layer](../ExpenseTrackerAPI.Infrastructure/README.md)** - Repository implementations
- **[Contracts Layer](../ExpenseTrackerAPI.Contracts/README.md)** - Request/Response DTOs
- **[WebApi Layer](../ExpenseTrackerAPI.WebApi/README.md)** - API endpoints

### Testing
- **[Domain Tests](../../tests/ExpenseTrackerAPI.Domain.Tests/README.md)** - Tests for these validation rules

---

## Summary

The Domain layer enforces strict validation rules to ensure data integrity:

### User Validation
- ✅ Name: required, max 100 chars
- ✅ Email: required, max 254 chars, valid format, no consecutive dots
- ✅ Password Hash: required (complexity checked before hashing)
- ✅ Initial Balance: optional, any decimal value

### Transaction Validation
- ✅ User ID: required, positive integer
- ✅ Type: required, EXPENSE or INCOME
- ✅ Amount: required, positive decimal
- ✅ Date: required, between 1900-01-01 and now+1year
- ✅ Subject: required, max 255 chars
- ✅ Notes: optional, unlimited length
- ✅ Payment Method: required, valid enum
- ✅ Category ID: optional, positive if provided
- ✅ Transaction Group ID: optional

**Core Principle:** Invalid entities cannot be constructed. All validation happens at construction time with clear, descriptive error messages.