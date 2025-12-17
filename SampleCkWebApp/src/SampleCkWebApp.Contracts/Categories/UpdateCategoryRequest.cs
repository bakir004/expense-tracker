namespace SampleCkWebApp.Categories;

/// <summary>
/// Request model for updating an existing category
/// </summary>
public class UpdateCategoryRequest
{
    /// <summary>
    /// Category name (1-255 characters, required)
    /// </summary>
    /// <example>Food &amp; Dining</example>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Category description (optional)
    /// </summary>
    /// <example>Groceries, restaurants, and food delivery</example>
    public string? Description { get; set; }
    
    /// <summary>
    /// Category icon (optional, max 100 characters)
    /// </summary>
    /// <example>🍔</example>
    public string? Icon { get; set; }
}

