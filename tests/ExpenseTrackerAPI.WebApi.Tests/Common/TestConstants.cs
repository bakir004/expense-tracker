namespace ExpenseTrackerAPI.WebApi.Tests.Common;

/// <summary>
/// Shared constants used across all test classes.
/// </summary>
public static class TestConstants
{
    /// <summary>
    /// API versioning and routes.
    /// </summary>
    public static class Routes
    {
        public const string ApiVersion = "v1";
        public const string BaseUrl = "/api/v1";

        // Auth endpoints
        public const string AuthRegister = $"{BaseUrl}/auth/register";
        public const string AuthLogin = $"{BaseUrl}/auth/login";
        public const string AuthHealth = $"{BaseUrl}/auth/health";

        // Transaction endpoints
        public const string Transactions = $"{BaseUrl}/transactions";
        public static string Transaction(int id) => $"{Transactions}/{id}";

        // User endpoints
        public const string UserProfile = $"{BaseUrl}/users/profile";
    }

    /// <summary>
    /// Test user credentials that meet password requirements.
    /// Password must have: uppercase, lowercase, digit, special char, min 8 chars.
    /// </summary>
    public static class TestUsers
    {
        // Default test user (used by TestAuthHandler)
        public const int DefaultUserId = 1;
        public const string DefaultUserEmail = "test.user@expense-tracker.local";
        public const string DefaultUserName = "Test User";

        // Seeded test users (from DatabaseSeeder)
        public const string SeededUserPassword = "Password123!";
        public const string SeededUser1Email = "john.doe@email.com";
        public const string SeededUser1Name = "John Doe";
        public const string SeededUser2Email = "jane.smith@email.com";
        public const string SeededUser2Name = "Jane Smith";

        // Aliases for the primary seeded user (user 1)
        public const string SeededUserEmail = SeededUser1Email;
        public const string SeededUserName = SeededUser1Name;

        // New user for registration tests
        public const string NewUserEmail = "new.user@expense-tracker-test.local";
        public const string NewUserName = "New Test User";
        public const string NewUserPassword = "TestP@ssw0rd!";

        // Alternative user for conflict tests
        public const string AltUserEmail = "alt.user@expense-tracker-test.local";
        public const string AltUserName = "Alternative User";
    }

    /// <summary>
    /// Default transaction test values.
    /// </summary>
    public static class Transactions
    {
        public const decimal DefaultAmount = 100.50m;
        public const string DefaultSubject = "Test Transaction";
        public const string DefaultNotes = "Test notes for transaction";
        public const string UpdatedSubject = "Updated Transaction";
        public const decimal UpdatedAmount = 250.75m;
    }

    /// <summary>
    /// Common error messages for assertions.
    /// </summary>
    public static class ErrorMessages
    {
        public const string InvalidCredentials = "Invalid email or password";
        public const string UserNotFound = "User not found";
        public const string TransactionNotFound = "Transaction not found";
        public const string EmailAlreadyExists = "Email already exists";
    }
}
