// ============================================================================
// FILE: UserResponse.cs
// ============================================================================
// WHAT: API contract DTO (Data Transfer Object) for user information responses.
//
// WHY: This DTO exists in the Contracts layer to define how user data is
//      returned to API consumers. It's separate from the domain entity (User)
//      to control what information is exposed. Notice that PasswordHash is
//      intentionally excluded for security - this is why we need a separate
//      response DTO rather than returning the domain entity directly.
//
// WHAT IT DOES:
//      - Defines the structure of user data returned by GET /users and GET /users/{id}
//      - Contains safe-to-expose properties: Id, Name, Email, CreatedAt, UpdatedAt
//      - Excludes sensitive data (PasswordHash) from API responses
//      - Includes XML documentation for Swagger/OpenAPI generation
//      - Mapped from User domain entity using UserMappings.ToResponse()
//      - Acts as the contract for how user data appears in API responses
// ============================================================================

namespace ExpenseTrackerAPI.Users;

/// <summary>
/// User information response model
/// </summary>
public class UserResponse
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }
    
    /// <summary>
    /// User's full name
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// User's email address
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the user was created
    /// </summary>
    /// <example>2025-01-01T10:00:00Z</example>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Timestamp when the user was last updated
    /// </summary>
    /// <example>2025-01-01T10:00:00Z</example>
    public DateTime UpdatedAt { get; set; }
}

