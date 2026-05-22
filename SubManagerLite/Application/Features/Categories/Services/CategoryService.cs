using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Interfaces;
using SubManagerLite.Application.Features.Categories.Models;
using SubManagerLite.Infrastructure;

namespace SubManagerLite.Application.Features.Categories.Services;

public sealed class CategoryService(ApplicationDbContext db) : ICategoryService
{
    public async Task<List<CategoryResponse>> GetAllAsync(CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .Select(ToCategoryResponse)
            .ToListAsync(ct);
    }
    
    public async Task<CategoryResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Categories
            .AsNoTracking()
            .Select(ToCategoryResponse)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
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
            await db.AddAsync(category, ct);
            await db.SaveChangesAsync(ct);
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
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return false;
        
        if (request.Name is not null)
            category.Name = request.Name;
        if (request.ClearColor)
            category.Color = null;
        else if (request.Color is not null)
            category.Color = request.Color;
         
        db.Categories.Update(category);
        await db.SaveChangesAsync(ct);
        
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return false;
         
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        
        return true;
    }

    private static readonly Expression<Func<Category, CategoryResponse>> ToCategoryResponse =
        category => new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Color = category.Color
        };

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