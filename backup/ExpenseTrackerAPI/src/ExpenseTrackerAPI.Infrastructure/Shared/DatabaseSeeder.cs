using ExpenseTrackerAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTrackerAPI.Infrastructure.Shared;

/// <summary>
/// Seeds the database with demo data when empty. Used after EF Core migrations on startup.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// BCrypt hash for password "password123" (same as legacy database.sql seed).
    /// </summary>
    private const string SeedPasswordHash = "$2a$11$yQRQSx3N6m00FZPwo/uQiOhMyxf/pKAtSiijU6EoXKQtrGv5WvNF.";

    public static async Task SeedIfEmptyAsync(ExpenseTrackerDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
            return;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Users (password: password123)
        var users = new[]
        {
            new User { Name = "John Doe", Email = "john.doe@email.com", PasswordHash = SeedPasswordHash, InitialBalance = 0, CreatedAt = now, UpdatedAt = now },
            new User { Name = "Jane Smith", Email = "jane.smith@email.com", PasswordHash = SeedPasswordHash, InitialBalance = 0, CreatedAt = now, UpdatedAt = now },
            new User { Name = "Mike Wilson", Email = "mike.wilson@email.com", PasswordHash = SeedPasswordHash, InitialBalance = 0, CreatedAt = now, UpdatedAt = now }
        };
        context.Users.AddRange(users);
        await context.SaveChangesAsync(cancellationToken);

        // Categories (order matches legacy: 1=Food, 2=Transport, 3=Housing, 4=Entertainment, 5=Healthcare, 6=Shopping, 7=Education, 8=Bills, 9=Salary, 10=Investment, 11=Freelance, 12=Other Income)
        var categories = new[]
        {
            new Category { Name = "Food & Dining", Description = "Groceries, restaurants, and food delivery", Icon = "🍔" },
            new Category { Name = "Transportation", Description = "Gas, public transit, car maintenance", Icon = "🚗" },
            new Category { Name = "Housing", Description = "Rent, mortgage, utilities, maintenance", Icon = "🏠" },
            new Category { Name = "Entertainment", Description = "Movies, games, subscriptions, hobbies", Icon = "🎮" },
            new Category { Name = "Healthcare", Description = "Medical expenses, pharmacy, insurance", Icon = "⚕️" },
            new Category { Name = "Shopping", Description = "Clothing, electronics, general shopping", Icon = "🛍️" },
            new Category { Name = "Education", Description = "Courses, books, tuition", Icon = "📚" },
            new Category { Name = "Bills & Utilities", Description = "Electricity, water, internet, phone", Icon = "💡" },
            new Category { Name = "Salary", Description = "Regular employment income", Icon = "💰" },
            new Category { Name = "Investment", Description = "Dividends, capital gains, interest", Icon = "📈" },
            new Category { Name = "Freelance", Description = "Contract work, side gigs", Icon = "💼" },
            new Category { Name = "Other Income", Description = "Gifts, refunds, misc income", Icon = "🎁" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync(cancellationToken);

        // Transaction groups (user 1: Europe Trip, Home Renovation; user 2: Wedding Planning)
        var groups = new[]
        {
            new TransactionGroup { Name = "Europe Trip 2024", Description = "Summer vacation expenses and travel income", UserId = users[0].Id, CreatedAt = now },
            new TransactionGroup { Name = "Home Renovation", Description = "Kitchen remodel project", UserId = users[0].Id, CreatedAt = now },
            new TransactionGroup { Name = "Wedding Planning", Description = "Wedding-related transactions", UserId = users[1].Id, CreatedAt = now }
        };
        context.TransactionGroups.AddRange(groups);
        await context.SaveChangesAsync(cancellationToken);

        // Category IDs 1-12 after save
        int Cat(int index) => categories[index].Id;
        int UserId(int index) => users[index].Id;
        int GroupId(int index) => groups[index].Id;

        // User 1 (John) - Salary, groceries, gas, rent, freelance, flight, bonus
        // User 2 (Jane) - Salary, doctor, dividend, wedding deposit
        var transactions = new List<Transaction>
        {
            CreateTransaction(UserId(0), TransactionType.INCOME, 3500m, 3500m, 3500m, today.AddDays(-30), "Monthly salary", null, PaymentMethod.BANK_TRANSFER, Cat(8), "ABC Corporation", null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 50m, -50m, 3450m, today.AddDays(-30), "Grocery shopping", "Weekly groceries at Whole Foods", PaymentMethod.DEBIT_CARD, Cat(0), null, null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 60m, -60m, 3390m, today.AddDays(-29), "Gas station fill-up", null, PaymentMethod.CREDIT_CARD, Cat(1), null, null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 1200m, -1200m, 2190m, today.AddDays(-28), "Monthly rent payment", null, PaymentMethod.BANK_TRANSFER, Cat(2), null, null, now),
            CreateTransaction(UserId(0), TransactionType.INCOME, 500m, 500m, 2690m, today.AddDays(-26), "Freelance project", null, PaymentMethod.PAYPAL, Cat(10), "Client Project", null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 350m, -350m, 2340m, today.AddDays(-24), "Flight tickets", "Round trip to Paris", PaymentMethod.CREDIT_CARD, Cat(1), null, GroupId(0), now),
            CreateTransaction(UserId(0), TransactionType.INCOME, 1000m, 1000m, 3340m, today.AddDays(-21), "Bonus payment", null, PaymentMethod.BANK_TRANSFER, Cat(8), "ABC Corporation", null, now),
            CreateTransaction(UserId(1), TransactionType.INCOME, 4200m, 4200m, 4200m, today.AddDays(-30), "Monthly salary", null, PaymentMethod.BANK_TRANSFER, Cat(8), "XYZ Tech Inc", null, now),
            CreateTransaction(UserId(1), TransactionType.EXPENSE, 150m, -150m, 4050m, today.AddDays(-26), "Doctor visit copay", null, PaymentMethod.DEBIT_CARD, Cat(4), null, null, now),
            CreateTransaction(UserId(1), TransactionType.INCOME, 150m, 150m, 4200m, today.AddDays(-23), "Stock dividend", null, PaymentMethod.BANK_TRANSFER, Cat(9), "Investment Portfolio", null, now),
            CreateTransaction(UserId(1), TransactionType.EXPENSE, 500m, -500m, 3700m, today.AddDays(-21), "Wedding venue deposit", "Initial deposit for the reception venue", PaymentMethod.BANK_TRANSFER, Cat(3), null, GroupId(2), now)
        };

        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes all transactions and re-inserts seed transactions. Use in tests so each test starts with known data.
    /// Assumes users 1–3, categories 1–12, and transaction groups 1–3 exist.
    /// </summary>
    public static async Task SeedTransactionsAsync(ExpenseTrackerDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Transactions.ExecuteDeleteAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        int Cat(int i) => i + 1;
        int UserId(int i) => i + 1;
        int GroupId(int i) => i + 1;
        var transactions = new List<Transaction>
        {
            CreateTransaction(UserId(0), TransactionType.INCOME, 3500m, 3500m, 3500m, today.AddDays(-30), "Monthly salary", null, PaymentMethod.BANK_TRANSFER, Cat(8), "ABC Corporation", null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 50m, -50m, 3450m, today.AddDays(-30), "Grocery shopping", "Weekly groceries at Whole Foods", PaymentMethod.DEBIT_CARD, Cat(0), null, null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 60m, -60m, 3390m, today.AddDays(-29), "Gas station fill-up", null, PaymentMethod.CREDIT_CARD, Cat(1), null, null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 1200m, -1200m, 2190m, today.AddDays(-28), "Monthly rent payment", null, PaymentMethod.BANK_TRANSFER, Cat(2), null, null, now),
            CreateTransaction(UserId(0), TransactionType.INCOME, 500m, 500m, 2690m, today.AddDays(-26), "Freelance project", null, PaymentMethod.PAYPAL, Cat(10), "Client Project", null, now),
            CreateTransaction(UserId(0), TransactionType.EXPENSE, 350m, -350m, 2340m, today.AddDays(-24), "Flight tickets", "Round trip to Paris", PaymentMethod.CREDIT_CARD, Cat(1), null, GroupId(0), now),
            CreateTransaction(UserId(0), TransactionType.INCOME, 1000m, 1000m, 3340m, today.AddDays(-21), "Bonus payment", null, PaymentMethod.BANK_TRANSFER, Cat(8), "ABC Corporation", null, now),
            CreateTransaction(UserId(1), TransactionType.INCOME, 4200m, 4200m, 4200m, today.AddDays(-30), "Monthly salary", null, PaymentMethod.BANK_TRANSFER, Cat(8), "XYZ Tech Inc", null, now),
            CreateTransaction(UserId(1), TransactionType.EXPENSE, 150m, -150m, 4050m, today.AddDays(-26), "Doctor visit copay", null, PaymentMethod.DEBIT_CARD, Cat(4), null, null, now),
            CreateTransaction(UserId(1), TransactionType.INCOME, 150m, 150m, 4200m, today.AddDays(-23), "Stock dividend", null, PaymentMethod.BANK_TRANSFER, Cat(9), "Investment Portfolio", null, now),
            CreateTransaction(UserId(1), TransactionType.EXPENSE, 500m, -500m, 3700m, today.AddDays(-21), "Wedding venue deposit", "Initial deposit for the reception venue", PaymentMethod.BANK_TRANSFER, Cat(3), null, GroupId(2), now)
        };
        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static Transaction CreateTransaction(
        int userId,
        TransactionType type,
        decimal amount,
        decimal signedAmount,
        decimal cumulativeDelta,
        DateOnly date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int categoryId,
        string? incomeSource,
        int? transactionGroupId,
        DateTime now)
    {
        return new Transaction
        {
            UserId = userId,
            TransactionType = type,
            Amount = amount,
            SignedAmount = signedAmount,
            CumulativeDelta = cumulativeDelta,
            Date = date,
            Subject = subject,
            Notes = notes,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            TransactionGroupId = transactionGroupId,
            IncomeSource = incomeSource,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
