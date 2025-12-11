using SampleCkWebApp.Application.Users.Data;
using SampleCkWebApp.Users;

namespace SampleCkWebApp.WebApi.Mappings;

public static class UserMappings
{
    public static GetUsersResponse ToResponse(this GetUsersResult result)
    {
        return new GetUsersResponse
        {
            Users = result.Users.Select(u => u.ToResponse()).ToList(),
            TotalCount = result.Users.Count
        };
    }
    
    public static UserResponse ToResponse(this UserRecord record)
    {
        return new UserResponse
        {
            Id = record.Id,
            Name = record.Name,
            Email = record.Email,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}

