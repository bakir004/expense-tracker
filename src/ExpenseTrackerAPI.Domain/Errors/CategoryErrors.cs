using ErrorOr;

namespace ExpenseTrackerAPI.Domain.Errors;

/// <summary>
/// Domain-specific errors for category operations.
/// </summary>
public static class CategoryErrors
{
    public static Error NotFound =>
        Error.NotFound("Category", "Category not found.");

    public static Error DuplicateName =>
        Error.Conflict("Name", "A category with this name already exists.");

    public static Error InvalidName =>
        Error.Validation("Name", "Category name is required and must be between 1 and 255 characters.");
}
