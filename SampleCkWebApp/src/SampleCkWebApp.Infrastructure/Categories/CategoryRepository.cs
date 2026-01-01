// ============================================================================
// FILE: CategoryRepository.cs
// ============================================================================
// WHAT: Entity Framework Core implementation of the category repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for categories. It implements ICategoryRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      Uses Entity Framework Core for all operations since categories only
//      require simple CRUD operations with no complex queries.
//
// WHAT IT DOES:
//      - Implements ICategoryRepository interface with Entity Framework Core
//      - Uses EF Core LINQ for all CRUD operations on categories
//      - Handles database exceptions (unique violations, foreign key violations)
//      - Returns ErrorOr results for consistent error handling
// ============================================================================

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Categories.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.Categories;

/// <summary>
/// Entity Framework Core implementation of the category repository.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ExpenseTrackerDbContext _context;

    public CategoryRepository(ExpenseTrackerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<List<Category>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Id)
                .ToListAsync(cancellationToken);

            return categories;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve categories: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category == null)
            {
                return CategoryErrors.NotFound;
            }

            return category;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve category: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

            if (category == null)
            {
                return CategoryErrors.NotFound;
            }

            return category;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve category by name: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> CreateAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            return CategoryErrors.DuplicateName;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create category: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);

            if (existingCategory == null)
            {
                return CategoryErrors.NotFound;
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.Icon = category.Icon;

            await _context.SaveChangesAsync(cancellationToken);

            return existingCategory;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            return CategoryErrors.DuplicateName;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update category: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category == null)
            {
                return CategoryErrors.NotFound;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Deleted;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
        {
            return Error.Conflict("Database.Error", "Cannot delete category because it is referenced by expenses.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete category: {ex.Message}");
        }
    }
}
