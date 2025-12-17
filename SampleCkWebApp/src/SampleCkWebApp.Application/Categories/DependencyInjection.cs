// ============================================================================
// FILE: DependencyInjection.cs
// ============================================================================
// WHAT: Dependency injection registration extension methods for Category application services.
//
// WHY: This file exists in the Application layer to centralize the registration
//      of Category-related services. It follows the convention of having a
//      DependencyInjection class per feature/module. This keeps service
//      registration organized and makes it easy to add or remove Category
//      functionality. The extension method (AddCategoriesApplication)
//      makes it discoverable and easy to use in the WebApi layer.
//
// WHAT IT DOES:
//      - Registers ICategoryService with its implementation (CategoryService)
//      - Uses TryAddScoped to ensure the service is only registered once
//      - Provides AddCategoriesApplication() extension method for IServiceCollection
//      - Called from WebApi.DependencyInjection.AddApplication() to wire up
//        all Category application services
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleCkWebApp.Application.Categories.Interfaces.Application;

namespace SampleCkWebApp.Application.Categories;

public static class DependencyInjection
{
    public static IServiceCollection AddCategoriesApplication(this IServiceCollection services)
    {
        services.TryAddScoped<ICategoryService, CategoryService>();
        return services;
    }
}

