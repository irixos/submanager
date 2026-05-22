using System.Linq.Expressions;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Models;

namespace SubManagerLite.Application.Features.Categories;

public static class CategoryMappings
{
    public static readonly Expression<Func<Category, CategoryResponse>> ToCategoryResponse =
        category => new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color
        };

    public static CategoryResponse MapToCategoryResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color
        };
    }
}