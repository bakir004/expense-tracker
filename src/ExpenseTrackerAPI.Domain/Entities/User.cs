using System.Text.RegularExpressions;

namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents a user in the expense tracking system.
/// </summary>
public partial class User
{
    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9._%+-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?)*\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    /// <summary>
    /// The user's starting balance when they began tracking.
    /// Actual balance = InitialBalance + his last transaction's CumulativeDelta
    /// </summary>
    public decimal InitialBalance { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private User()
    {
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    /// <summary>
    /// Creates a new user with validation.
    /// </summary>
    /// <param name="name">User's display name (required, max 100 characters)</param>
    /// <param name="email">User's email address (required, must be valid format)</param>
    /// <param name="passwordHash">Hashed password (required)</param>
    /// <param name="initialBalance">Starting balance (optional, defaults to 0)</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public User(string name, string email, string passwordHash, decimal initialBalance = 0)
    {
        // Normalize inputs before validation
        var normalizedName = name?.Trim() ?? string.Empty;
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        ValidateBusinessRules(normalizedName, normalizedEmail, passwordHash);

        Name = normalizedName;
        Email = normalizedEmail;
        PasswordHash = passwordHash;
        InitialBalance = initialBalance;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    /// <param name="name">New display name</param>
    /// <param name="email">New email address</param>
    public void UpdateProfile(string name, string email)
    {
        // Normalize inputs before validation
        var normalizedName = name?.Trim() ?? string.Empty;
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        ValidateBusinessRules(normalizedName, normalizedEmail, PasswordHash);

        Name = normalizedName;
        Email = normalizedEmail;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's password hash.
    /// </summary>
    /// <param name="passwordHash">New hashed password</param>
    public void UpdatePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's initial balance.
    /// </summary>
    /// <param name="initialBalance">New initial balance</param>
    public void UpdateInitialBalance(decimal initialBalance)
    {
        InitialBalance = initialBalance;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateBusinessRules(string name, string email, string passwordHash)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (name.Trim().Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters", nameof(name));

        // Email validation
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (email.Length > 254) // RFC 5321 limit
            throw new ArgumentException("Email cannot exceed 254 characters", nameof(email));

        if (!EmailRegex().IsMatch(email) || email.Contains(".."))
            throw new ArgumentException("Email format is invalid", nameof(email));

        // Password hash validation
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));
    }
}
