using ErrorOr;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Application;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Categories;

/// <summary>
/// Application service for category operations.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    /// <inheritdoc />
    public async Task<ErrorOr<List<Category>>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _categoryRepository.GetAllAsync(cancellationToken);
    }
}
