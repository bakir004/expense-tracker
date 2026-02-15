namespace ExpenseTrackerAPI.Contracts.Categories;

/// <summary>
/// Response contract for category data.
/// </summary>
/// <param name="Id">Unique identifier for the category</param>
/// <param name="Name">Category name (e.g., "Food and Dining", "Transport", "Salary")</param>
/// <param name="Description">Optional detailed description of the category's purpose or scope</param>
/// <param name="Icon">Icon identifier or emoji for visual representation in the UI</param>
public record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    string Icon);
