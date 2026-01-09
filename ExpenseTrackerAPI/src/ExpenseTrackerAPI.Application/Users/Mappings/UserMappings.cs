// ============================================================================
// FILE: UserMappings.cs
// ============================================================================
// WHAT: Extension methods for mapping domain entities and application DTOs to API contracts.
//
// WHY: This mapping class exists in the Application layer because it needs to
//      know about both domain entities (User) and API contracts (UserResponse,
//      GetUsersResponse). The Application layer is responsible for transforming
//      domain models into what the outside world (API) needs. By placing mappings
//      here, we keep controllers thin and make mappings reusable across different
//      presentation layers (WebApi, gRPC, etc.) if needed.
//
// WHAT IT DOES:
//      - Provides ToResponse() extension methods for mapping:
//        * User domain entity -> UserResponse (API contract)
//        * GetUsersResult (application DTO) -> GetUsersResponse (API contract)
//      - Excludes sensitive data (password hash) from API responses
//      - Used by controllers to transform service results into API responses
//      - Keeps presentation layer (WebApi) free of mapping logic
// ============================================================================

using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Application.Users.Data;
using ExpenseTrackerAPI.Users;

namespace ExpenseTrackerAPI.Application.Users.Mappings;

/// <summary>
/// Mapping extensions for converting domain entities and application DTOs to API contracts.
/// </summary>
public static class UserMappings
{
    /// <summary>
    /// Maps GetUsersResult (application layer) to GetUsersResponse (API contract)
    /// </summary>
    public static GetUsersResponse ToResponse(this GetUsersResult result)
    {
        return new GetUsersResponse
        {
            Users = result.Users.Select(u => u.ToResponse()).ToList(),
            TotalCount = result.Users.Count
        };
    }
    
    /// <summary>
    /// Maps User domain entity to UserResponse DTO.
    /// Note: Password hash is intentionally excluded for security.
    /// </summary>
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}

