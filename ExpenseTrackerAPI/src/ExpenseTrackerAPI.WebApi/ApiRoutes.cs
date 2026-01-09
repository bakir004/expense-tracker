namespace ExpenseTrackerAPI.WebApi;

/// <summary>
/// Centralized API route constants for versioning.
/// Add new version prefixes here when creating new API versions.
/// </summary>
public static class ApiRoutes
{
    public const string V1 = "api/v1";
    public const string V2 = "api/v2";
    
    /// <summary>
    /// V1 route templates
    /// </summary>
    public static class V1Routes
    {
        public const string Users = $"{V1}/users";
        public const string Categories = $"{V1}/categories";
        public const string Transactions = $"{V1}/transactions";
        public const string TransactionGroups = $"{V1}/transaction-groups";
    }
    
    /// <summary>
    /// V2 route templates (for future use)
    /// </summary>
    public static class V2Routes
    {
        public const string Users = $"{V2}/users";
        public const string Categories = $"{V2}/categories";
        public const string Transactions = $"{V2}/transactions";
        public const string TransactionGroups = $"{V2}/transaction-groups";
    }
}

