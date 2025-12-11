namespace SampleCkWebApp.Users;

public class GetUsersResponse
{
    public List<UserResponse> Users { get; set; } = new();
    
    public int TotalCount { get; set; }
}

