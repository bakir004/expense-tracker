namespace SampleCkWebApp.ExpenseGroups;

public class CreateExpenseGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserId { get; set; }
}

