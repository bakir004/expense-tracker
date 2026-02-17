using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ExpenseTrackerAPI.Infrastructure.Shared;

/// <summary>
/// Seeds the database with demo data when empty. Used after EF Core migrations on startup.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Generate BCrypt hash for seed password "Password123!" - meets complexity requirements.
    /// </summary>
    private static string GenerateSeedPasswordHash() => BCrypt.Net.BCrypt.HashPassword("Password123!");

    public static async Task SeedIfEmptyAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
            return;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Users (password: Password123!)
        var seedPasswordHash = GenerateSeedPasswordHash();
        var users = new[]
        {
            new User("John Doe", "john.doe@email.com", seedPasswordHash, 0),
            new User("Jane Smith", "jane.smith@email.com", seedPasswordHash, 0),
            new User("Mike Wilson", "mike.wilson@email.com", seedPasswordHash, 0)
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
            t1.UpdateCumulativeDelta(3500m);
            transactions.Add(t1);

            var t2 = new Transaction(UserId(0), TransactionType.EXPENSE, 50m, today.AddDays(-30), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t2.UpdateCumulativeDelta(3450m);
            transactions.Add(t2);

            var t3 = new Transaction(UserId(0), TransactionType.EXPENSE, 60m, today.AddDays(-29), "Gas station fill-up", PaymentMethod.CREDIT_CARD, null, Cat(1), null);
            t3.UpdateCumulativeDelta(3390m);
            transactions.Add(t3);

            var t4 = new Transaction(UserId(0), TransactionType.EXPENSE, 1200m, today.AddDays(-28), "Monthly rent payment", PaymentMethod.BANK_TRANSFER, null, Cat(2), null);
            t4.UpdateCumulativeDelta(2190m);
            transactions.Add(t4);

            var t5 = new Transaction(UserId(0), TransactionType.INCOME, 500m, today.AddDays(-26), "Freelance project", PaymentMethod.PAYPAL, null, Cat(10), null);
            t5.UpdateCumulativeDelta(2690m);
            transactions.Add(t5);

            var t6 = new Transaction(UserId(0), TransactionType.EXPENSE, 350m, today.AddDays(-24), "Flight tickets", PaymentMethod.CREDIT_CARD, "Round trip to Paris", Cat(1), GroupId(0));
            t6.UpdateCumulativeDelta(2340m);
            transactions.Add(t6);

            var t7 = new Transaction(UserId(0), TransactionType.INCOME, 1000m, today.AddDays(-21), "Bonus payment", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t7.UpdateCumulativeDelta(3340m);
            transactions.Add(t7);

            // User 2 transactions
            var t8 = new Transaction(UserId(1), TransactionType.INCOME, 4200m, today.AddDays(-30), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t8.UpdateCumulativeDelta(4200m);
            transactions.Add(t8);

            var t9 = new Transaction(UserId(1), TransactionType.EXPENSE, 150m, today.AddDays(-26), "Doctor visit copay", PaymentMethod.DEBIT_CARD, null, Cat(4), null);
            t9.UpdateCumulativeDelta(4050m);
            transactions.Add(t9);

            var t10 = new Transaction(UserId(1), TransactionType.INCOME, 150m, today.AddDays(-23), "Stock dividend", PaymentMethod.BANK_TRANSFER, null, Cat(9), null);
            t10.UpdateCumulativeDelta(4200m);
            transactions.Add(t10);

            var t11 = new Transaction(UserId(1), TransactionType.EXPENSE, 500m, today.AddDays(-21), "Wedding venue deposit", PaymentMethod.BANK_TRANSFER, "Initial deposit for the reception venue", Cat(3), GroupId(2));
            t11.UpdateCumulativeDelta(3700m);
            transactions.Add(t11);

                        var t12 = new Transaction(UserId(0), TransactionType.EXPENSE, 85m, today.AddDays(-20), "Restaurant dinner", PaymentMethod.CREDIT_CARD, "Date night at Italian bistro", Cat(5), null);
            t12.UpdateCumulativeDelta(3255m); // 3340 - 85
            transactions.Add(t12);

            var t13 = new Transaction(UserId(0), TransactionType.EXPENSE, 45m, today.AddDays(-19), "Streaming subscriptions", PaymentMethod.CREDIT_CARD, "Netflix, Spotify bundle", Cat(6), null);
            t13.UpdateCumulativeDelta(3210m); // 3255 - 45
            transactions.Add(t13);

            var t14 = new Transaction(UserId(0), TransactionType.EXPENSE, 120m, today.AddDays(-18), "Electricity bill", PaymentMethod.BANK_TRANSFER, null, Cat(7), null);
            t14.UpdateCumulativeDelta(3090m); // 3210 - 120
            transactions.Add(t14);

            var t15 = new Transaction(UserId(0), TransactionType.EXPENSE, 75m, today.AddDays(-17), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t15.UpdateCumulativeDelta(3015m); // 3090 - 75
            transactions.Add(t15);

            var t16 = new Transaction(UserId(0), TransactionType.EXPENSE, 200m, today.AddDays(-16), "Gym annual membership", PaymentMethod.CREDIT_CARD, null, Cat(3), null);
            t16.UpdateCumulativeDelta(2815m); // 3015 - 200
            transactions.Add(t16);

            var t17 = new Transaction(UserId(0), TransactionType.INCOME, 250m, today.AddDays(-15), "Online course sale", PaymentMethod.PAYPAL, null, Cat(10), null);
            t17.UpdateCumulativeDelta(3065m); // 2815 + 250
            transactions.Add(t17);

            var t18 = new Transaction(UserId(0), TransactionType.EXPENSE, 30m, today.AddDays(-14), "Coffee shop work session", PaymentMethod.DEBIT_CARD, "Weekly remote work at café", Cat(5), null);
            t18.UpdateCumulativeDelta(3035m); // 3065 - 30
            transactions.Add(t18);

            var t19 = new Transaction(UserId(0), TransactionType.EXPENSE, 95m, today.AddDays(-13), "Pharmacy prescriptions", PaymentMethod.DEBIT_CARD, null, Cat(4), null);
            t19.UpdateCumulativeDelta(2940m); // 3035 - 95
            transactions.Add(t19);

            var t20 = new Transaction(UserId(0), TransactionType.EXPENSE, 400m, today.AddDays(-12), "Hotel booking", PaymentMethod.CREDIT_CARD, "2 nights Paris hotel", Cat(1), GroupId(0));
            t20.UpdateCumulativeDelta(2540m); // 2940 - 400
            transactions.Add(t20);

            var t21 = new Transaction(UserId(0), TransactionType.EXPENSE, 60m, today.AddDays(-11), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Midweek top-up at Trader Joe's", Cat(0), null);
            t21.UpdateCumulativeDelta(2480m); // 2540 - 60
            transactions.Add(t21);

            var t22 = new Transaction(UserId(0), TransactionType.INCOME, 180m, today.AddDays(-10), "Cashback reward redemption", PaymentMethod.BANK_TRANSFER, null, Cat(9), null);
            t22.UpdateCumulativeDelta(2660m); // 2480 + 180
            transactions.Add(t22);

            var t23 = new Transaction(UserId(0), TransactionType.EXPENSE, 55m, today.AddDays(-9), "Internet bill", PaymentMethod.BANK_TRANSFER, null, Cat(7), null);
            t23.UpdateCumulativeDelta(2605m); // 2660 - 55
            transactions.Add(t23);

            var t24 = new Transaction(UserId(0), TransactionType.EXPENSE, 140m, today.AddDays(-8), "New shoes", PaymentMethod.CREDIT_CARD, "Running shoes from Nike outlet", Cat(3), null);
            t24.UpdateCumulativeDelta(2465m); // 2605 - 140
            transactions.Add(t24);

            var t25 = new Transaction(UserId(0), TransactionType.EXPENSE, 70m, today.AddDays(-7), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t25.UpdateCumulativeDelta(2395m); // 2465 - 70
            transactions.Add(t25);

            var t26 = new Transaction(UserId(0), TransactionType.INCOME, 600m, today.AddDays(-6), "Freelance project", PaymentMethod.PAYPAL, "UI design for startup client", Cat(10), null);
            t26.UpdateCumulativeDelta(2995m); // 2395 + 600
            transactions.Add(t26);

            var t27 = new Transaction(UserId(0), TransactionType.EXPENSE, 25m, today.AddDays(-5), "Parking fees", PaymentMethod.DEBIT_CARD, null, Cat(1), null);
            t27.UpdateCumulativeDelta(2970m); // 2995 - 25
            transactions.Add(t27);

            var t28 = new Transaction(UserId(0), TransactionType.EXPENSE, 90m, today.AddDays(-4), "Dinner with friends", PaymentMethod.CREDIT_CARD, "Birthday celebration at steakhouse", Cat(5), null);
            t28.UpdateCumulativeDelta(2880m); // 2970 - 90
            transactions.Add(t28);

            var t29 = new Transaction(UserId(0), TransactionType.EXPENSE, 110m, today.AddDays(-3), "Phone bill", PaymentMethod.BANK_TRANSFER, null, Cat(7), null);
            t29.UpdateCumulativeDelta(2770m); // 2880 - 110
            transactions.Add(t29);

            var t30 = new Transaction(UserId(0), TransactionType.EXPENSE, 65m, today.AddDays(-2), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekend grocery run", Cat(0), null);
            t30.UpdateCumulativeDelta(2705m); // 2770 - 65
            transactions.Add(t30);

            var t31 = new Transaction(UserId(0), TransactionType.INCOME, 3500m, today.AddDays(-1), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t31.UpdateCumulativeDelta(6205m); // 2705 + 3500
            transactions.Add(t31);

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
            t1.UpdateCumulativeDelta(3500m); transactions.Add(t1);

            var t2 = new Transaction(UserId(0), TransactionType.EXPENSE, 50m, today.AddDays(-30), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t2.UpdateCumulativeDelta(3450m); transactions.Add(t2);

            var t3 = new Transaction(UserId(0), TransactionType.EXPENSE, 60m, today.AddDays(-29), "Gas station fill-up", PaymentMethod.CREDIT_CARD, null, Cat(1), null);
            t3.UpdateCumulativeDelta(3390m); transactions.Add(t3);

            var t4 = new Transaction(UserId(0), TransactionType.EXPENSE, 1200m, today.AddDays(-28), "Monthly rent payment", PaymentMethod.BANK_TRANSFER, null, Cat(2), null);
            t4.UpdateCumulativeDelta(2190m); transactions.Add(t4);

            var t5 = new Transaction(UserId(0), TransactionType.INCOME, 500m, today.AddDays(-26), "Freelance project", PaymentMethod.PAYPAL, null, Cat(10), null);
            t5.UpdateCumulativeDelta(2690m); transactions.Add(t5);

            var t6 = new Transaction(UserId(0), TransactionType.EXPENSE, 350m, today.AddDays(-24), "Flight tickets", PaymentMethod.CREDIT_CARD, "Round trip to Paris", Cat(1), GroupId(0));
            t6.UpdateCumulativeDelta(2340m); transactions.Add(t6);

            var t7 = new Transaction(UserId(0), TransactionType.INCOME, 1000m, today.AddDays(-21), "Bonus payment", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t7.UpdateCumulativeDelta(3340m); transactions.Add(t7);

            var t8 = new Transaction(UserId(1), TransactionType.INCOME, 4200m, today.AddDays(-30), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t8.UpdateCumulativeDelta(4200m); transactions.Add(t8);

            var t9 = new Transaction(UserId(1), TransactionType.EXPENSE, 150m, today.AddDays(-26), "Doctor visit copay", PaymentMethod.DEBIT_CARD, null, Cat(4), null);
            t9.UpdateCumulativeDelta(4050m); transactions.Add(t9);

            var t10 = new Transaction(UserId(1), TransactionType.INCOME, 150m, today.AddDays(-23), "Stock dividend", PaymentMethod.BANK_TRANSFER, null, Cat(9), null);
            t10.UpdateCumulativeDelta(4200m); transactions.Add(t10);

            var t11 = new Transaction(UserId(1), TransactionType.EXPENSE, 500m, today.AddDays(-21), "Wedding venue deposit", PaymentMethod.BANK_TRANSFER, "Initial deposit for the reception venue", Cat(3), GroupId(2));
            t11.UpdateCumulativeDelta(3700m); transactions.Add(t11);

            var t12 = new Transaction(UserId(0), TransactionType.EXPENSE, 85m, today.AddDays(-20), "Restaurant dinner", PaymentMethod.CREDIT_CARD, "Date night at Italian bistro", Cat(5), null);
            t12.UpdateCumulativeDelta(3255m); // 3340 - 85
            transactions.Add(t12);

            var t13 = new Transaction(UserId(0), TransactionType.EXPENSE, 45m, today.AddDays(-19), "Streaming subscriptions", PaymentMethod.CREDIT_CARD, "Netflix, Spotify bundle", Cat(6), null);
            t13.UpdateCumulativeDelta(3210m); // 3255 - 45
            transactions.Add(t13);

            var t14 = new Transaction(UserId(0), TransactionType.EXPENSE, 120m, today.AddDays(-18), "Electricity bill", PaymentMethod.BANK_TRANSFER, null, Cat(7), null);
            t14.UpdateCumulativeDelta(3090m); // 3210 - 120
            transactions.Add(t14);

            var t15 = new Transaction(UserId(0), TransactionType.EXPENSE, 75m, today.AddDays(-17), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t15.UpdateCumulativeDelta(3015m); // 3090 - 75
            transactions.Add(t15);

            var t16 = new Transaction(UserId(0), TransactionType.EXPENSE, 200m, today.AddDays(-16), "Gym annual membership", PaymentMethod.CREDIT_CARD, null, Cat(3), null);
            t16.UpdateCumulativeDelta(2815m); // 3015 - 200
            transactions.Add(t16);

            var t17 = new Transaction(UserId(0), TransactionType.INCOME, 250m, today.AddDays(-15), "Online course sale", PaymentMethod.PAYPAL, null, Cat(10), null);
            t17.UpdateCumulativeDelta(3065m); // 2815 + 250
            transactions.Add(t17);

            var t18 = new Transaction(UserId(0), TransactionType.EXPENSE, 30m, today.AddDays(-14), "Coffee shop work session", PaymentMethod.DEBIT_CARD, "Weekly remote work at café", Cat(5), null);
            t18.UpdateCumulativeDelta(3035m); // 3065 - 30
            transactions.Add(t18);

            var t19 = new Transaction(UserId(0), TransactionType.EXPENSE, 95m, today.AddDays(-13), "Pharmacy prescriptions", PaymentMethod.DEBIT_CARD, null, Cat(4), null);
            t19.UpdateCumulativeDelta(2940m); // 3035 - 95
            transactions.Add(t19);

            var t20 = new Transaction(UserId(0), TransactionType.EXPENSE, 400m, today.AddDays(-12), "Hotel booking", PaymentMethod.CREDIT_CARD, "2 nights Paris hotel", Cat(1), GroupId(0));
            t20.UpdateCumulativeDelta(2540m); // 2940 - 400
            transactions.Add(t20);

            var t21 = new Transaction(UserId(0), TransactionType.EXPENSE, 60m, today.AddDays(-11), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Midweek top-up at Trader Joe's", Cat(0), null);
            t21.UpdateCumulativeDelta(2480m); // 2540 - 60
            transactions.Add(t21);

            var t22 = new Transaction(UserId(0), TransactionType.INCOME, 180m, today.AddDays(-10), "Cashback reward redemption", PaymentMethod.BANK_TRANSFER, null, Cat(9), null);
            t22.UpdateCumulativeDelta(2660m); // 2480 + 180
            transactions.Add(t22);

            var t23 = new Transaction(UserId(0), TransactionType.EXPENSE, 55m, today.AddDays(-9), "Internet bill", PaymentMethod.BANK_TRANSFER, null, Cat(7), null);
            t23.UpdateCumulativeDelta(2605m); // 2660 - 55
            transactions.Add(t23);

            var t24 = new Transaction(UserId(0), TransactionType.EXPENSE, 140m, today.AddDays(-8), "New shoes", PaymentMethod.CREDIT_CARD, "Running shoes from Nike outlet", Cat(3), null);
            t24.UpdateCumulativeDelta(2465m); // 2605 - 140
            transactions.Add(t24);

            var t25 = new Transaction(UserId(0), TransactionType.EXPENSE, 70m, today.AddDays(-7), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekly groceries at Whole Foods", Cat(0), null);
            t25.UpdateCumulativeDelta(2395m); // 2465 - 70
            transactions.Add(t25);

            var t26 = new Transaction(UserId(0), TransactionType.INCOME, 600m, today.AddDays(-6), "Freelance project", PaymentMethod.PAYPAL, "UI design for startup client", Cat(10), null);
            t26.UpdateCumulativeDelta(2995m); // 2395 + 600
            transactions.Add(t26);

            var t27 = new Transaction(UserId(0), TransactionType.EXPENSE, 25m, today.AddDays(-5), "Parking fees", PaymentMethod.DEBIT_CARD, null, Cat(1), null);
            t27.UpdateCumulativeDelta(2970m); // 2995 - 25
            transactions.Add(t27);

            var t28 = new Transaction(UserId(0), TransactionType.EXPENSE, 90m, today.AddDays(-4), "Dinner with friends", PaymentMethod.CREDIT_CARD, "Birthday celebration at steakhouse", Cat(5), null);
            t28.UpdateCumulativeDelta(2880m); // 2970 - 90
            transactions.Add(t28);

            var t29 = new Transaction(UserId(0), TransactionType.EXPENSE, 110m, today.AddDays(-3), "Phone bill", PaymentMethod.BANK_TRANSFER, null, Cat(7), null);
            t29.UpdateCumulativeDelta(2770m); // 2880 - 110
            transactions.Add(t29);

            var t30 = new Transaction(UserId(0), TransactionType.EXPENSE, 65m, today.AddDays(-2), "Grocery shopping", PaymentMethod.DEBIT_CARD, "Weekend grocery run", Cat(0), null);
            t30.UpdateCumulativeDelta(2705m); // 2770 - 65
            transactions.Add(t30);

            var t31 = new Transaction(UserId(0), TransactionType.INCOME, 3500m, today.AddDays(-1), "Monthly salary", PaymentMethod.BANK_TRANSFER, null, Cat(8), null);
            t31.UpdateCumulativeDelta(6205m); // 2705 + 3500
            transactions.Add(t31);

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
