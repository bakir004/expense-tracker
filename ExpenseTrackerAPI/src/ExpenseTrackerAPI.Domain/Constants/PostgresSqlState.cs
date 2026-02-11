namespace ExpenseTrackerAPI.Domain.Constants;

/// <summary>
/// PostgreSQL SQLSTATE error codes (standard / PostgreSQL).
/// Used when mapping database exceptions to domain errors in Infrastructure.
/// </summary>
public static class PostgresSqlState
{
    /// <summary>Foreign key violation: insert/update references a non-existent parent row.</summary>
    public const string ForeignKeyViolation = "23503";

    /// <summary>Unique violation: duplicate key value violates a unique constraint.</summary>
    public const string UniqueViolation = "23505";
}
