using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Categories;

/// <summary>
/// Extension methods for mapping Category entities to response contracts.
/// </summary>
public static class CategoryMappingExtensions
{
    /// <summary>
    /// Maps a Category entity to a CategoryResponse contract.
    /// </summary>
    /// <param name="category">The category entity to map.</param>
    /// <returns>A CategoryResponse containing the category data.</returns>
    public static CategoryResponse ToResponse(this Category category)
    {
        return new CategoryResponse(
            Id: category.Id,
            Name: category.Name,
            Description: category.Description,
            Icon: category.Icon);
    }

    /// <summary>
    /// Maps a collection of Category entities to CategoryResponse contracts.
    /// </summary>
    /// <param name="categories">The category entities to map.</param>
    /// <returns>A collection of CategoryResponse contracts.</returns>
    public static IEnumerable<CategoryResponse> ToResponses(this IEnumerable<Category> categories)
    {
        return categories.Select(c => c.ToResponse());
    }
}
