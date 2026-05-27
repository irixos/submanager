using System.Linq.Expressions;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Categories.Models;

namespace SubManager.Api.Application.Features.Categories;

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