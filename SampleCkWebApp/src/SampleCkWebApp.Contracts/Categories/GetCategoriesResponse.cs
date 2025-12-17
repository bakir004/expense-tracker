namespace SampleCkWebApp.Categories;

/// <summary>
/// Response model containing a list of categories
/// </summary>
public class GetCategoriesResponse
{
    /// <summary>
    /// List of categories
    /// </summary>
    public List<CategoryResponse> Categories { get; set; } = new();
    
    /// <summary>
    /// Total number of categories in the response
    /// </summary>
    /// <example>8</example>
    public int TotalCount { get; set; }
}

