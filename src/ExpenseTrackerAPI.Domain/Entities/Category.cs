namespace ExpenseTrackerAPI.Domain.Entities;

/// <summary>
/// Represents a category in the expense tracker system.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Icon { get; set; }
}
