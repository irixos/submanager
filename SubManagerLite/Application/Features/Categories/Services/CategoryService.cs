using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Interfaces;
using SubManagerLite.Application.Features.Categories.Models;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Application.Features.Categories.Services;

public sealed class CategoryService(ICategoryRepository categoryRepository) : ICategoryService
{
    public async Task<List<CategoryResponse>> GetAllAsync(CancellationToken ct)
    {
        var categories = await categoryRepository.GetAllAsync(ct);
        
        var response = categories.Select(MapToCategoryResponse).ToList();
        
        return response;
    }
    
    public async Task<CategoryResponse?> GetAsync(int id, CancellationToken ct)
    {
        var channel = await categoryRepository.GetAsync(id, ct);
        if (channel is null) return null;

        var response = MapToCategoryResponse(channel);

        return response;
    }
    
    public async Task<CategoryResponse?> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {

        var category = new Category
        {
            Name = request.Name,
            Color = request.Color,
        };

        try
        {
            await categoryRepository.AddAsync(category, ct);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        var response = MapToCategoryResponse(category);
        
        return response;
    }

    public async Task<bool> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryRepository.GetAsync(id, ct);
        if (category is null) return false;
        
        if (request.Name is not null)
            category.Name = request.Name;
        if (request.ClearColor)
            category.Color = null;
        else if (request.Color is not null)
            category.Color = request.Color;
         
        await categoryRepository.UpdateAsync(category, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await categoryRepository.GetAsync(id, ct);
        if (category is null) return false;
         
        await categoryRepository.DeleteAsync(category, ct);
        return true;
    }

    private static CategoryResponse MapToCategoryResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color
        };
    }
}