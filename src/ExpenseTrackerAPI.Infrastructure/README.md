# ExpenseTracker Infrastructure Layer

This document explains the Infrastructure layer of the ExpenseTracker API, with detailed focus on the transaction balance tracking system and cumulative delta calculations.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Cumulative Delta System](#cumulative-delta-system)
- [Transaction Operations Deep Dive](#transaction-operations-deep-dive)
- [Database Schema](#database-schema)
- [Repositories](#repositories)
- [Performance Considerations](#performance-considerations)

---

## Overview

The Infrastructure layer implements the data access and persistence logic for the ExpenseTracker API. It uses:

- **Entity Framework Core** for ORM
- **PostgreSQL** as the database
- **Bulk update operations** for efficient balance recalculations
- **Database transactions** for consistency
- **Retry policies** for resilience

**Key Responsibility:** Maintain accurate running balances for all transactions using a cumulative delta approach.

---

## Architecture

### Layer Structure

```
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs          # EF Core DbContext
│   └── Configurations/                   # Entity configurations
│       ├── TransactionConfiguration.cs
│       ├── UserConfiguration.cs
│       ├── CategoryConfiguration.cs
│       └── TransactionGroupConfiguration.cs
├── Transactions/
│   └── TransactionRepository.cs          # Transaction CRUD + balance logic
├── Users/
│   └── UserRepository.cs
├── Categories/
│   └── CategoryRepository.cs
├── TransactionGroups/
│   └── TransactionGroupRepository.cs
├── Authentication/
│   └── JwtTokenGenerator.cs
└── Shared/
    └── DatabaseSeeder.cs
```

---

## Cumulative Delta System

### Design Philosophy

Instead of maintaining a separate balance table or recalculating balances from scratch on every query, we use a **cumulative delta** approach:

```
Actual Balance = User.InitialBalance + Transaction.CumulativeDelta
```

### What is Cumulative Delta?

**Cumulative Delta** is a running sum of all signed transaction amounts up to and including a specific transaction, ordered chronologically.

**Example:**

| Date       | Transaction | Amount | Signed Amount | Cumulative Delta |
|------------|-------------|--------|---------------|------------------|
| 2024-01-01 | Income      | +500   | +500          | 500              |
| 2024-01-05 | Expense     | -50    | -50           | 450              |
| 2024-01-10 | Expense     | -100   | -100          | 350              |
| 2024-01-15 | Income      | +200   | +200          | 550              |

If the user's initial balance is $1000:
- After transaction 1: Balance = 1000 + 500 = **$1500**
- After transaction 2: Balance = 1000 + 450 = **$1450**
- After transaction 3: Balance = 1000 + 350 = **$1350**
- After transaction 4: Balance = 1000 + 550 = **$1550**

### Why This Approach?

**Advantages:**
1. ✅ **Fast balance queries** - No need to sum transactions, just read one field
2. ✅ **Efficient updates** - Only affected transactions need recalculation
3. ✅ **Point-in-time balance** - Every transaction knows the balance at that moment
4. ✅ **Audit trail** - Historical balances are preserved
5. ✅ **No separate balance table** - Simpler schema, fewer tables

**Trade-offs:**
- ⚠️ Requires recalculating affected transactions on create/update/delete
- ⚠️ More complex update logic for date changes

---

## Transaction Operations Deep Dive

### Overview of Operations

All transaction mutations (Create, Update, Delete) follow this pattern:

1. **Start a database transaction** (for ACID guarantees)
2. **Calculate the new cumulative delta** for the affected transaction
3. **Save the transaction change**
4. **Bulk update all subsequent transactions** (those that come after chronologically)
5. **Commit or rollback** based on success/failure

### 1. Create Transaction

**Goal:** Insert a new transaction and update all transactions that come after it.

#### Algorithm

```
1. Find the previous transaction (chronologically before the new one)
2. Calculate: new_cumulative_delta = previous_cumulative_delta + signed_amount
3. Insert the new transaction with calculated cumulative_delta
4. Bulk update all subsequent transactions: cumulative_delta += signed_amount
5. Commit
```

#### Code Flow

```csharp
public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction)
{
    // Start DB transaction for consistency
    await using var dbTransaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Find the previous transaction's cumulative delta
        var previousCumulativeDelta = await _context.Transactions
            .Where(t => t.UserId == transaction.UserId)
            .Where(t => t.Date < transaction.Date ||
                       (t.Date == transaction.Date && t.CreatedAt < transaction.CreatedAt))
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => t.CumulativeDelta)
            .FirstOrDefaultAsync();

        // 2. Calculate cumulative delta for new transaction
        transaction.UpdateCumulativeDelta(previousCumulativeDelta + transaction.SignedAmount);

        // 3. Insert the transaction
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // 4. Bulk update all subsequent transactions
        await _context.Transactions
            .Where(t => t.UserId == transaction.UserId)
            .Where(t => t.Date > transaction.Date ||
                       (t.Date == transaction.Date && t.CreatedAt > transaction.CreatedAt))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + transaction.SignedAmount)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

        await dbTransaction.CommitAsync();
        return transaction;
    }
    catch
    {
        await dbTransaction.RollbackAsync();
        throw;
    }
}
```

#### Example

**Before:**
```
User has InitialBalance = $1000

Existing transactions:
- 2024-01-01: +$500 → CumulativeDelta = $500  (Balance: $1500)
- 2024-01-10: -$100 → CumulativeDelta = $400  (Balance: $1400)
```

**Create new transaction:** 2024-01-05, -$50 (expense)

**After:**
```
- 2024-01-01: +$500 → CumulativeDelta = $500  (Balance: $1500) [unchanged]
- 2024-01-05: -$50  → CumulativeDelta = $450  (Balance: $1450) [NEW]
- 2024-01-10: -$100 → CumulativeDelta = $350  (Balance: $1350) [updated: 400 - 50]
```

---

### 2. Update Transaction

**Goal:** Modify a transaction and recalculate affected cumulative deltas.

This is the **most complex operation** because we need to handle:
1. **Amount changes** - affects all subsequent transactions
2. **Date changes** - affects transactions in between old and new dates
3. **Both changes simultaneously**

#### Optimization: Fast Path

If **only the amount changed** (date stayed the same):

```csharp
decimal amountDelta = newSignedAmount - oldSignedAmount;
transaction.CumulativeDelta = oldCumulativeDelta + amountDelta;

// Bulk update all subsequent transactions
UPDATE Transactions
SET cumulative_delta = cumulative_delta + amountDelta
WHERE user_id = ? AND (date > ? OR (date = ? AND created_at > ?))
```

#### Complex Case: Date Changed

When a transaction's date changes, we need to understand the "ripple effect":

**Visual Representation:**

```
Timeline: --------P----N------>
          (P = old position, N = new position)

Moving FORWARD in time (P → N):
- Transactions between P and N: subtract the signed_amount
- Transactions after N: apply correction (if amount also changed)

Moving BACKWARD in time (N → P):
- Transactions between N and P: add the signed_amount
- Transactions after P: apply correction (if amount also changed)
```

#### Algorithm for Date Change

```
1. Determine minDate and maxDate (old and new dates)
2. Calculate previousCumulativeDelta (transaction before new position)
3. Calculate correctionAmount = newSignedAmount - oldSignedAmount
4. Update the transaction's cumulative_delta:
   - If moving into PAST: previousCumulativeDelta + newSignedAmount
   - If moving into FUTURE: previousCumulativeDelta + correctionAmount
5. Update transactions in the affected range [minDate, maxDate]:
   - If moving into PAST: add signedAmount
   - If moving into FUTURE: subtract oldSignedAmount
6. If amount changed, update all transactions after maxDate with correctionAmount
7. Commit
```

#### Code Flow for Date Change

```csharp
public async Task<ErrorOr<Transaction>> UpdateAsync(
    Transaction oldTransaction,
    Transaction transaction)
{
    await using var dbTransaction = await _context.Database.BeginTransactionAsync();

    try
    {
        bool dateChanged = oldTransaction.Date != transaction.Date;
        bool amountChanged = oldTransaction.SignedAmount != transaction.SignedAmount;

        // Fast path: no date or amount change
        if (!dateChanged && !amountChanged)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return transaction;
        }

        // Fast path: only amount changed
        if (!dateChanged && amountChanged)
        {
            decimal amountDelta = transaction.SignedAmount - oldTransaction.SignedAmount;
            transaction.UpdateCumulativeDelta(oldTransaction.CumulativeDelta + amountDelta);
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();

            // Update all subsequent transactions
            await _context.Transactions
                .Where(t => t.UserId == transaction.UserId)
                .Where(t => t.Date > transaction.Date ||
                           (t.Date == transaction.Date && t.CreatedAt > transaction.CreatedAt))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + amountDelta)
                    .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

            await dbTransaction.CommitAsync();
            return transaction;
        }

        // Complex path: date changed (with or without amount change)
        var minDate = oldTransaction.Date < transaction.Date ? oldTransaction.Date : transaction.Date;
        var maxDate = oldTransaction.Date > transaction.Date ? oldTransaction.Date : transaction.Date;

        // Find previous transaction at new position
        var previousCumulativeDelta = await _context.Transactions
            .Where(t => t.UserId == transaction.UserId)
            .Where(t => t.Date < transaction.Date ||
                       (t.Date == transaction.Date && t.CreatedAt < transaction.CreatedAt))
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => t.CumulativeDelta)
            .FirstOrDefaultAsync();

        var correctionAmount = transaction.SignedAmount - oldTransaction.SignedAmount;

        // Calculate new cumulative delta based on direction of move
        if (transaction.Date < oldTransaction.Date)
        {
            // Moving into the PAST
            transaction.UpdateCumulativeDelta(previousCumulativeDelta + transaction.SignedAmount);
        }
        else
        {
            // Moving into the FUTURE
            transaction.UpdateCumulativeDelta(previousCumulativeDelta + correctionAmount);
        }

        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();

        // Update transactions in the affected range
        var deltaAmount = transaction.Date < oldTransaction.Date
            ? transaction.SignedAmount      // Moving into past: add
            : -oldTransaction.SignedAmount; // Moving into future: subtract

        await _context.Transactions
            .Where(t => t.UserId == transaction.UserId)
            .Where(t => t.Date > minDate && t.Date < maxDate
                || (t.Date == minDate && t.CreatedAt > transaction.CreatedAt)
                || (t.Date == maxDate && t.CreatedAt < transaction.CreatedAt))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + deltaAmount)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

        // If amount also changed, update transactions after maxDate
        if (amountChanged)
        {
            await _context.Transactions
                .Where(t => t.UserId == transaction.UserId)
                .Where(t => t.Date > maxDate ||
                           (t.Date == maxDate && t.CreatedAt > transaction.CreatedAt))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + correctionAmount)
                    .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));
        }

        await dbTransaction.CommitAsync();
        return transaction;
    }
    catch
    {
        await dbTransaction.RollbackAsync();
        throw;
    }
}
```

#### Example: Update with Date Change

**Before:**
```
User InitialBalance = $1000

- 2024-01-01: +$500 → CumulativeDelta = $500  (Balance: $1500)
- 2024-01-05: -$50  → CumulativeDelta = $450  (Balance: $1450)
- 2024-01-10: -$100 → CumulativeDelta = $350  (Balance: $1350)
```

**Update:** Move 2024-01-05 transaction to 2024-01-15 (keep amount -$50)

**Step-by-step:**
1. Transaction moves from 2024-01-05 to 2024-01-15 (forward in time)
2. minDate = 2024-01-05, maxDate = 2024-01-15
3. previousCumulativeDelta at new position (after 2024-01-10) = $350
4. Moving forward, so new cumulative_delta = 350 + 0 = $350 (correctionAmount = 0)
5. Transactions between 2024-01-05 and 2024-01-15 (only 2024-01-10):
   - Subtract oldSignedAmount (-50): 350 - (-50) = $400

**After:**
```
- 2024-01-01: +$500 → CumulativeDelta = $500  (Balance: $1500) [unchanged]
- 2024-01-10: -$100 → CumulativeDelta = $400  (Balance: $1400) [updated: 350 + 50]
- 2024-01-15: -$50  → CumulativeDelta = $350  (Balance: $1350) [moved]
```

---

### 3. Delete Transaction

**Goal:** Remove a transaction and update all subsequent transactions.

#### Algorithm

```
1. Find the transaction to delete
2. Bulk update all subsequent transactions: cumulative_delta -= signed_amount
3. Delete the transaction
4. Commit
```

#### Code Flow

```csharp
public async Task<ErrorOr<Deleted>> DeleteAsync(int id)
{
    await using var dbTransaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Find the transaction
        var transaction = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return TransactionErrors.NotFound;
        }

        // 2. Update all subsequent transactions
        await _context.Transactions
            .Where(t => t.UserId == transaction.UserId)
            .Where(t => t.Date > transaction.Date ||
                       (t.Date == transaction.Date && t.CreatedAt > transaction.CreatedAt))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta - transaction.SignedAmount)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

        // 3. Delete the transaction
        await _context.Transactions
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync();

        await dbTransaction.CommitAsync();
        return Result.Deleted;
    }
    catch
    {
        await dbTransaction.RollbackAsync();
        throw;
    }
}
```

#### Example

**Before:**
```
User InitialBalance = $1000

- 2024-01-01: +$500 → CumulativeDelta = $500  (Balance: $1500)
- 2024-01-05: -$50  → CumulativeDelta = $450  (Balance: $1450)
- 2024-01-10: -$100 → CumulativeDelta = $350  (Balance: $1350)
```

**Delete:** 2024-01-05 transaction (-$50)

**After:**
```
- 2024-01-01: +$500 → CumulativeDelta = $500  (Balance: $1500) [unchanged]
- 2024-01-10: -$100 → CumulativeDelta = $400  (Balance: $1400) [updated: 350 + 50]
```

---

## Database Schema

### Transaction Table

```sql
CREATE TABLE transactions (
    id                    SERIAL PRIMARY KEY,
    user_id               INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    transaction_type      VARCHAR(20) NOT NULL,  -- 'EXPENSE' or 'INCOME'
    amount                DECIMAL(12,2) NOT NULL CHECK (amount > 0),
    signed_amount         DECIMAL(12,2) NOT NULL,  -- Negative for expenses, positive for income
    date                  DATE NOT NULL,
    subject               VARCHAR(255) NOT NULL,
    notes                 TEXT,
    payment_method        VARCHAR(20) NOT NULL,
    cumulative_delta      DECIMAL(12,2) NOT NULL,  -- Running balance
    category_id           INTEGER REFERENCES categories(id) ON DELETE RESTRICT,
    transaction_group_id  INTEGER REFERENCES transaction_groups(id) ON DELETE SET NULL,
    created_at            TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at            TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Critical index for chronological ordering and queries
CREATE INDEX idx_transaction_user_date 
    ON transactions(user_id, date DESC, id DESC);

-- Index for filtering by type
CREATE INDEX idx_transaction_type 
    ON transactions(user_id, transaction_type);

-- Index for group filtering
CREATE INDEX idx_transaction_group 
    ON transactions(transaction_group_id);
```

### Key Indexes

1. **`idx_transaction_user_date`** - Composite index on `(user_id, date DESC, id DESC)`
   - Used for finding previous/next transactions
   - Essential for bulk updates of subsequent transactions
   - Descending order matches query patterns

2. **`idx_transaction_type`** - Index on `(user_id, transaction_type)`
   - Used for filtering expenses vs income
   - Common query pattern for reports

3. **`idx_transaction_group`** - Index on `transaction_group_id`
   - Used for grouping transactions
   - Foreign key index

---

## Repositories

### TransactionRepository

**Location:** `Infrastructure/Transactions/TransactionRepository.cs`

**Key Methods:**
- `GetByIdAsync` - Retrieve single transaction
- `CreateAsync` - Create transaction + update balances
- `UpdateAsync` - Update transaction + recalculate balances
- `DeleteAsync` - Delete transaction + update balances
- `GetByUserIdWithFilterAsync` - Query with filtering, sorting, pagination

### UserRepository

**Location:** `Infrastructure/Users/UserRepository.cs`

**Key Methods:**
- `GetByIdAsync` - Get user by ID
- `GetByEmailAsync` - Get user by email (for login)
- `CreateAsync` - Create new user
- `UpdateAsync` - Update user profile
- `DeleteAsync` - Delete user (cascades to transactions)

### CategoryRepository

**Location:** `Infrastructure/Categories/CategoryRepository.cs`

**Key Methods:**
- `GetAllAsync` - Get all categories
- `GetByIdAsync` - Get single category

### TransactionGroupRepository

**Location:** `Infrastructure/TransactionGroups/TransactionGroupRepository.cs`

**Key Methods:**
- `GetByIdAsync` - Get single group
- `GetByUserIdAsync` - Get all groups for user
- `CreateAsync` - Create new group
- `UpdateAsync` - Update group
- `DeleteAsync` - Delete group (transactions become ungrouped)

---

## Performance Considerations

### Why Bulk Updates Are Fast

We use **EF Core's `ExecuteUpdateAsync`** for bulk operations:

```csharp
await _context.Transactions
    .Where(t => t.UserId == userId)
    .Where(t => t.Date > someDate)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(t => t.CumulativeDelta, t => t.CumulativeDelta + delta));
```

This generates a **single SQL UPDATE statement**:
```sql
UPDATE transactions
SET cumulative_delta = cumulative_delta + @delta,
    updated_at = @now
WHERE user_id = @userId AND date > @someDate;
```

**Benefits:**
- ✅ Single database round trip
- ✅ No loading entities into memory
- ✅ Database-side calculation
- ✅ Transaction-safe

### Execution Strategy

All mutation operations use `CreateExecutionStrategy()` for automatic retry on transient failures:

```csharp
var strategy = _context.Database.CreateExecutionStrategy();
return await strategy.ExecuteAsync(async () => {
    // Database operations here
});
```

Configured in `ApplicationDbContext`:
```csharp
optionsBuilder.UseNpgsql(options =>
{
    options.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorCodesToAdd: null);
});
```

### Transaction Isolation

All mutation operations use database transactions:
```csharp
await using var dbTransaction = await _context.Database.BeginTransactionAsync();
try
{
    // Operations
    await dbTransaction.CommitAsync();
}
catch
{
    await dbTransaction.RollbackAsync();
    throw;
}
```

This ensures **ACID properties**:
- **Atomicity** - All or nothing
- **Consistency** - Cumulative deltas always correct
- **Isolation** - No dirty reads
- **Durability** - Changes are permanent

### Query Performance

**Fast Balance Queries:**
```csharp
// Current balance: O(1) lookup
var latestTransaction = await _context.Transactions
    .Where(t => t.UserId == userId)
    .OrderByDescending(t => t.Date)
    .ThenByDescending(t => t.CreatedAt)
    .FirstOrDefaultAsync();

var balance = user.InitialBalance + latestTransaction.CumulativeDelta;
```

**No need to:**
- ❌ Sum all transactions (O(n))
- ❌ Maintain separate balance table
- ❌ Run scheduled recalculation jobs

---

## Error Handling

### Foreign Key Violations

Automatically detected and mapped to domain errors:

```csharp
catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && 
                                    pgEx.SqlState == PostgresSqlState.ForeignKeyViolation)
{
    var constraint = pgEx.ConstraintName ?? pgEx.Message;
    if (constraint.Contains("Users"))
        return UserErrors.NotFound;
    if (constraint.Contains("Category"))
        return CategoryErrors.NotFound;
    if (constraint.Contains("TransactionGroup"))
        return TransactionGroupErrors.NotFound;
}
```

### Concurrency

Database transactions prevent race conditions:
- Multiple users can create transactions simultaneously
- Bulk updates are atomic
- Retry policy handles transient failures

---

## Testing Considerations

### Key Test Scenarios

1. **Create in chronological order**
2. **Create out of chronological order** (past-dated transactions)
3. **Create future-dated transactions**
4. **Update amount only**
5. **Update date forward in time**
6. **Update date backward in time**
7. **Update both date and amount**
8. **Delete first transaction**
9. **Delete middle transaction**
10. **Delete last transaction**
11. **Multiple transactions on same date**

### Balance Verification

After any operation, verify:
```csharp
var transactions = await GetAllTransactionsForUser(userId);
decimal runningDelta = 0;

foreach (var tx in transactions.OrderBy(t => t.Date).ThenBy(t => t.CreatedAt))
{
    runningDelta += tx.SignedAmount;
    Assert.Equal(runningDelta, tx.CumulativeDelta);
}
```

---

## Additional Resources

### Related Documentation
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and layer responsibilities
- **[Domain Layer](../Domain/README.md)** - Business entities and validation rules
- **[Application Layer](../Application/README.md)** - Services using these repositories
- **[Contracts Layer](../Contracts/README.md)** - Request/Response DTOs
- **[WebApi Layer](../WebApi/README.md)** - API endpoints

### Testing
- **[Infrastructure Tests](../../tests/Infrastructure.Tests/README.md)** - Tests for repositories and cumulative delta

---

## Summary

The Infrastructure layer implements a **cumulative delta system** for tracking transaction balances:

- ✅ **Efficient** - O(1) balance queries, bulk updates for changes
- ✅ **Consistent** - Database transactions ensure correctness
- ✅ **Resilient** - Retry policies handle transient failures
- ✅ **Auditable** - Every transaction knows the balance at that point in time
- ✅ **Scalable** - No need for expensive recalculations

**Core Principle:** Every transaction stores a running balance (cumulative delta), and any change to a transaction automatically recalculates all affected subsequent transactions using efficient bulk updates.
