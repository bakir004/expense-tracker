namespace ExpenseTrackerAPI.Contracts.Categories;

/// <summary>
/// Response contract for category data.
/// </summary>
public record CategoryResponse(
    int Id,
    string Name,
    string? Description,
    string Icon);
