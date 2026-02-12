// ============================================================================
// FILE: DependencyInjection.cs
// ============================================================================
// WHAT: Dependency injection registration extension methods for Category infrastructure services.
//
// WHY: This file exists in the Infrastructure layer to centralize the registration
//      of Category-related infrastructure components (repository). It follows
//      the same pattern as other features - one DependencyInjection class
//      per feature. This keeps infrastructure setup organized and makes it easy
//      to configure Category persistence.
//
// WHAT IT DOES:
//      - Registers ICategoryRepository with its PostgreSQL implementation (CategoryRepository)
//      - Uses TryAddScoped to ensure services are only registered once
//      - Provides AddCategoriesInfrastructure() extension method for IServiceCollection
//      - Called from WebApi.DependencyInjection.AddInfrastructure() to set up
//        all Category infrastructure services
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;

namespace ExpenseTrackerAPI.Infrastructure.Categories;

public static class DependencyInjection
{
    public static IServiceCollection AddCategoriesInfrastructure(this IServiceCollection services)
    {
        services.TryAddScoped<ICategoryRepository, CategoryRepository>();
        
        return services;
    }
}

