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
        
        var response = categories.Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color
            })
            .ToList();
        
        return response;
    }
    public async Task<CategoryResponse?> GetAsync(int id, CancellationToken ct)
    {
        var channel = await categoryRepository.GetAsync(id, ct);
        if (channel == null) return null;

        var response = new CategoryResponse
        {
            Id = channel.Id,
            Name = channel.Name,
            Color = channel.Color
        };

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

        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color
        };
        
        return response;
    }

    public async Task<bool> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryRepository.GetAsync(id, ct);
        if (category == null) return false;
        
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
        if (category == null) return false;
         
        await categoryRepository.DeleteAsync(category, ct);
        return true;
    }
}