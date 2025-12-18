namespace SampleCkWebApp.Contracts.TransactionGroups;

public class TransactionGroupResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

