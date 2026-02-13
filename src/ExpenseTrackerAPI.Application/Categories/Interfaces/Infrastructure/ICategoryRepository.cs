using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;

/// <summary>
/// Repository interface for category persistence operations.
/// Defined in Application layer, implemented in Infrastructure layer.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Get all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all categories</returns>
    Task<ErrorOr<List<Category>>> GetAllAsync(CancellationToken cancellationToken);
}
