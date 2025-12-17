// ============================================================================
// FILE: CategoryMappings.cs
// ============================================================================
// WHAT: Extension methods for mapping domain entities and application DTOs to API contracts.
//
// WHY: This mapping class exists in the Application layer because it needs to
//      know about both domain entities (Category) and API contracts (CategoryResponse,
//      GetCategoriesResponse). The Application layer is responsible for transforming
//      domain models into what the outside world (API) needs.
//
// WHAT IT DOES:
//      - Provides ToResponse() extension methods for mapping:
//        * Category domain entity -> CategoryResponse (API contract)
//        * GetCategoriesResult (application DTO) -> GetCategoriesResponse (API contract)
//      - Used by controllers to transform service results into API responses
// ============================================================================

using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Categories.Data;
using SampleCkWebApp.Categories;

namespace SampleCkWebApp.Application.Categories.Mappings;

/// <summary>
/// Mapping extensions for converting domain entities and application DTOs to API contracts.
/// </summary>
public static class CategoryMappings
{
    /// <summary>
    /// Maps GetCategoriesResult (application layer) to GetCategoriesResponse (API contract)
    /// </summary>
    public static GetCategoriesResponse ToResponse(this GetCategoriesResult result)
    {
        return new GetCategoriesResponse
        {
            Categories = result.Categories.Select(c => c.ToResponse()).ToList(),
            TotalCount = result.Categories.Count
        };
    }
    
    /// <summary>
    /// Maps Category domain entity to CategoryResponse DTO.
    /// </summary>
    public static CategoryResponse ToResponse(this Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = category.Icon
        };
    }
}

