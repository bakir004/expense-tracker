using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Categories.Interfaces.Application;

/// <summary>
/// Service interface for category business operations.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Get all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    Task<ErrorOr<List<Category>>> GetAllAsync(CancellationToken cancellationToken);
}
