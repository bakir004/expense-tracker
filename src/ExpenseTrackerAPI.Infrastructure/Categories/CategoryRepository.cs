using ErrorOr;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;

namespace ExpenseTrackerAPI.Infrastructure.Categories;

/// <summary>
/// EF Core implementation of the category repository.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<ErrorOr<List<Category>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve categories: {ex.Message}");
        }
    }
}
