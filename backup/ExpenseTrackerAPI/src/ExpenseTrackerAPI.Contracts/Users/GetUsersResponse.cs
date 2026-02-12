// ============================================================================
// FILE: GetUsersResponse.cs
// ============================================================================
// WHAT: API contract DTO (Data Transfer Object) for GET /users endpoint response.
//
// WHY: This DTO exists in the Contracts layer to define the structure of
//      the response when retrieving multiple users. It wraps a list of
//      UserResponse objects along with metadata (TotalCount). Having a
//      dedicated response type rather than returning List<UserResponse>
//      directly allows for future extensibility (pagination, filtering
//      metadata, etc.) without breaking the API contract.
//
// WHAT IT DOES:
//      - Defines the response structure for GET /users endpoint
//      - Contains a list of UserResponse objects and a TotalCount
//      - Provides a structured way to return multiple users with metadata
//      - Mapped from GetUsersResult (application layer) using UserMappings
//      - Includes XML documentation for Swagger/OpenAPI generation
//      - Acts as the contract for bulk user retrieval API responses
// ============================================================================

namespace ExpenseTrackerAPI.Users;

/// <summary>
/// Response model containing a list of users
/// </summary>
public class GetUsersResponse
{
    /// <summary>
    /// List of users
    /// </summary>
    public List<UserResponse> Users { get; set; } = new();
    
    /// <summary>
    /// Total number of users in the response
    /// </summary>
    /// <example>10</example>
    public int TotalCount { get; set; }
}

