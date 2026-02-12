using ExpenseTrackerAPI.Infrastructure.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.IntegrationTests;

/// <summary>
/// Same as <see cref="DatabaseFixture"/> but clears the Transaction table in <see cref="DisposeAsync"/>.
/// Use only for transaction repository tests so other test classes (User, Category) are not affected.
/// Owns its own <see cref="DatabaseFixture"/>; xUnit does not inject one fixture into another.
/// </summary>
public sealed class TransactionDatabaseFixture : IAsyncLifetime, IAsyncDisposable
{
    private readonly DatabaseFixture _db = new();

    public IServiceProvider Services => _db.Services;

    public Task InitializeAsync() => _db.InitializeAsync();

    public async Task DisposeAsync()
    {
        try
        {
            await using var scope = _db.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ExpenseTrackerDbContext>();
            await context.Transactions.ExecuteDeleteAsync();
        }
        catch
        {
            // best-effort cleanup
        }
        await _db.DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();
}
