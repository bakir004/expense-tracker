// ============================================================================
// FILE: GetUsersResult.cs
// ============================================================================
// WHAT: Application layer DTO (Data Transfer Object) for GetUsers operation result.
//
// WHY: This DTO exists in the Application layer to structure the return value
//      from the GetUsersAsync operation. It wraps the list of users in a
//      result object that can be easily mapped to API contracts. Using a
//      dedicated result type rather than returning List<User> directly
//      allows for future extensibility (e.g., adding pagination metadata,
//      filters applied, etc.) without breaking the API contract.
//
// WHAT IT DOES:
//      - Contains a list of User domain entities
//      - Provides a structured way to return multiple users from the service
//      - Used by UserService.GetUsersAsync as the return type
//      - Mapped to GetUsersResponse (API contract) in UserMappings
// ============================================================================

using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Users.Data;

/// <summary>
/// Result DTO for GetUsers operation.
/// Contains a list of domain entities.
/// </summary>
public class GetUsersResult
{
    public List<User> Users { get; set; } = new();
}

