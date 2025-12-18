namespace SampleCkWebApp.Contracts.TransactionGroups;

public class CreateTransactionGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserId { get; set; }
}

