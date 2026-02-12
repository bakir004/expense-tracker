using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;
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

    public static async Task SeedIfEmptyAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
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
            new Category { Name = "Food & Dining", Description = "Groceries, restaurants, and food delivery", Icon = "🍔"},
            new Category { Name = "Transportation", Description = "Gas, public transit, car maintenance", Icon = "🚗"},
            new Category { Name = "Housing", Description = "Rent, mortgage, utilities, maintenance", Icon = "🏠"},
            new Category { Name = "Entertainment", Description = "Movies, games, subscriptions, hobbies", Icon = "🎮"},
            new Category { Name = "Healthcare", Description = "Medical expenses, pharmacy, insurance", Icon = "⚕️"},
            new Category { Name = "Shopping", Description = "Clothing, electronics, general shopping", Icon = "🛍️"},
            new Category { Name = "Education", Description = "Courses, books, tuition", Icon = "📚"},
            new Category { Name = "Bills & Utilities", Description = "Electricity, water, internet, phone", Icon = "💡"},
            new Category { Name = "Salary", Description = "Regular employment income", Icon = "💰"},
            new Category { Name = "Investment", Description = "Dividends, capital gains, interest", Icon = "📈"},
            new Category { Name = "Freelance", Description = "Contract work, side gigs", Icon = "💼"},
            new Category { Name = "Other Income", Description = "Gifts, refunds, misc income", Icon = "🎁"}
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
        var transactions = new List<Transaction>();

        // Create transactions using the constructor (which validates business rules)
        try
        {
            // User 1 transactions
            var t1 = new Transaction(UserId(0), TransactionType.INCOME, 3500m, today.AddDays(-30), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t1.CumulativeDelta = 3500m;
            t1.CreatedAt = now;
            t1.UpdatedAt = now;
            transactions.Add(t1);

            var t2 = new Transaction(UserId(0), TransactionType.EXPENSE, 50m, today.AddDays(-30), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t2.CumulativeDelta = 3450m;
            t2.CreatedAt = now;
            t2.UpdatedAt = now;
            transactions.Add(t2);

            var t3 = new Transaction(UserId(0), TransactionType.EXPENSE, 60m, today.AddDays(-29), "Gas station fill-up", PaymentMethod.CREDIT_CARD, null, Cat(1), null);
            t3.CumulativeDelta = 3390m;
            t3.CreatedAt = now;
            t3.UpdatedAt = now;
            transactions.Add(t3);

            var t4 = new Transaction(UserId(0), TransactionType.EXPENSE, 1200m, today.AddDays(-28), "Monthly rent payment", PaymentMethod.BANK_TRANSFER, null, Cat(2), null);
            t4.CumulativeDelta = 2190m;
            t4.CreatedAt = now;
            t4.UpdatedAt = now;
            transactions.Add(t4);

            var t5 = new Transaction(UserId(0), TransactionType.INCOME, 500m, today.AddDays(-26), "Freelance project", PaymentMethod.PAYPAL, null, Cat(10), null);
            t5.CumulativeDelta = 2690m;
            t5.CreatedAt = now;
            t5.UpdatedAt = now;
            transactions.Add(t5);

            var t6 = new Transaction(UserId(0), TransactionType.EXPENSE, 350m, today.AddDays(-24), "Flight tickets", PaymentMethod.CREDIT_CARD, "Round trip to Paris", Cat(1), GroupId(0));
            t6.CumulativeDelta = 2340m;
            t6.CreatedAt = now;
            t6.UpdatedAt = now;
            transactions.Add(t6);

            var t7 = new Transaction(UserId(0), TransactionType.INCOME, 1000m, today.AddDays(-21), "Bonus payment", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t7.CumulativeDelta = 3340m;
            t7.CreatedAt = now;
            t7.UpdatedAt = now;
            transactions.Add(t7);

            // User 2 transactions
            var t8 = new Transaction(UserId(1), TransactionType.INCOME, 4200m, today.AddDays(-30), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t8.CumulativeDelta = 4200m;
            t8.CreatedAt = now;
            t8.UpdatedAt = now;
            transactions.Add(t8);

            var t9 = new Transaction(UserId(1), TransactionType.EXPENSE, 150m, today.AddDays(-26), "Doctor visit copay", PaymentMethod.DEBIT_CARD, null, Cat(4), null);
            t9.CumulativeDelta = 4050m;
            t9.CreatedAt = now;
            t9.UpdatedAt = now;
            transactions.Add(t9);

            var t10 = new Transaction(UserId(1), TransactionType.INCOME, 150m, today.AddDays(-23), "Stock dividend", PaymentMethod.BANK_TRANSFER, null, Cat(9), null);
            t10.CumulativeDelta = 4200m;
            t10.CreatedAt = now;
            t10.UpdatedAt = now;
            transactions.Add(t10);

            var t11 = new Transaction(UserId(1), TransactionType.EXPENSE, 500m, today.AddDays(-21), "Wedding venue deposit", PaymentMethod.BANK_TRANSFER, "Initial deposit for the reception venue", Cat(3), GroupId(2));
            t11.CumulativeDelta = 3700m;
            t11.CreatedAt = now;
            t11.UpdatedAt = now;
            transactions.Add(t11);

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException ex)
        {
            // Log the validation error but don't crash the seeding process
            Console.WriteLine($"Validation error during seeding: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes all transactions and re-inserts seed transactions. Use in tests so each test starts with known data.
    /// Assumes users 1–3, categories 1–12, and transaction groups 1–3 exist.
    /// </summary>
    public static async Task SeedTransactionsAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Transactions.ExecuteDeleteAsync(cancellationToken);

        // Get existing entities by their expected order
        var users = await context.Users.OrderBy(u => u.Id).Take(3).ToListAsync(cancellationToken);
        var categories = await context.Categories.OrderBy(c => c.Id).Take(12).ToListAsync(cancellationToken);
        var groups = await context.TransactionGroups.OrderBy(g => g.Id).Take(3).ToListAsync(cancellationToken);

        if (users.Count < 2 || categories.Count < 12 || groups.Count < 3)
            return; // Not properly seeded

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        int Cat(int i) => categories[i].Id;
        int UserId(int i) => users[i].Id;
        int GroupId(int i) => groups[i].Id;

        var transactions = new List<Transaction>();

        try
        {
            // Recreate the same transactions as above but using existing IDs
            var t1 = new Transaction(UserId(0), TransactionType.INCOME, 3500m, today.AddDays(-30), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t1.CumulativeDelta = 3500m; t1.CreatedAt = now; t1.UpdatedAt = now; transactions.Add(t1);

            var t2 = new Transaction(UserId(0), TransactionType.EXPENSE, 50m, today.AddDays(-30), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t2.CumulativeDelta = 3450m; t2.CreatedAt = now; t2.UpdatedAt = now; transactions.Add(t2);

            var t3 = new Transaction(UserId(0), TransactionType.EXPENSE, 60m, today.AddDays(-29), "Gas station fill-up", PaymentMethod.CREDIT_CARD, null, Cat(1), null);
            t3.CumulativeDelta = 3390m; t3.CreatedAt = now; t3.UpdatedAt = now; transactions.Add(t3);

            var t4 = new Transaction(UserId(0), TransactionType.EXPENSE, 1200m, today.AddDays(-28), "Monthly rent payment", PaymentMethod.BANK_TRANSFER, null, Cat(2), null);
            t4.CumulativeDelta = 2190m; t4.CreatedAt = now; t4.UpdatedAt = now; transactions.Add(t4);

            var t5 = new Transaction(UserId(0), TransactionType.INCOME, 500m, today.AddDays(-26), "Freelance project", PaymentMethod.PAYPAL, null, Cat(10), null);
            t5.CumulativeDelta = 2690m; t5.CreatedAt = now; t5.UpdatedAt = now; transactions.Add(t5);

            var t6 = new Transaction(UserId(0), TransactionType.EXPENSE, 350m, today.AddDays(-24), "Flight tickets", PaymentMethod.CREDIT_CARD, "Round trip to Paris", Cat(1), GroupId(0));
            t6.CumulativeDelta = 2340m; t6.CreatedAt = now; t6.UpdatedAt = now; transactions.Add(t6);

            var t7 = new Transaction(UserId(0), TransactionType.INCOME, 1000m, today.AddDays(-21), "Bonus payment", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t7.CumulativeDelta = 3340m; t7.CreatedAt = now; t7.UpdatedAt = now; transactions.Add(t7);

            var t8 = new Transaction(UserId(1), TransactionType.INCOME, 4200m, today.AddDays(-30), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t8.CumulativeDelta = 4200m; t8.CreatedAt = now; t8.UpdatedAt = now; transactions.Add(t8);

            var t9 = new Transaction(UserId(1), TransactionType.EXPENSE, 150m, today.AddDays(-26), "Doctor visit copay", PaymentMethod.DEBIT_CARD, null, Cat(4), null);
            t9.CumulativeDelta = 4050m; t9.CreatedAt = now; t9.UpdatedAt = now; transactions.Add(t9);

            var t10 = new Transaction(UserId(1), TransactionType.INCOME, 150m, today.AddDays(-23), "Stock dividend", PaymentMethod.BANK_TRANSFER, null, Cat(9), null);
            t10.CumulativeDelta = 4200m; t10.CreatedAt = now; t10.UpdatedAt = now; transactions.Add(t10);

            var t11 = new Transaction(UserId(1), TransactionType.EXPENSE, 500m, today.AddDays(-21), "Wedding venue deposit", PaymentMethod.BANK_TRANSFER, "Initial deposit for the reception venue", Cat(3), GroupId(2));
            t11.CumulativeDelta = 3700m; t11.CreatedAt = now; t11.UpdatedAt = now; transactions.Add(t11);

            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Validation error during transaction seeding: {ex.Message}");
            throw;
        }
    }
}
