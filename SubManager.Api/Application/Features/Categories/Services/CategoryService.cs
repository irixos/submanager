using System.Linq.Expressions;
using Gridify;
using Gridify.EntityFramework;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Categories.Interfaces;
using SubManager.Api.Application.Features.Categories.Models;
using SubManager.Api.Infrastructure;

namespace SubManager.Api.Application.Features.Categories.Services;

public sealed class CategoryService(ApplicationDbContext db) : ICategoryService
{
    public async Task<Paging<CategoryResponse>> GetAllAsync(GridifyQuery query, CancellationToken ct)
    {
        return await db.Categories
            .Select(CategoryMappings.ToCategoryResponse)
            .GridifyAsync(query.ClampPageSize(), ct);
    }
    
    public async Task<CategoryResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Categories
            .Select(CategoryMappings.ToCategoryResponse)
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
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return null;
        }

        var response = CategoryMappings.MapToCategoryResponse(category);
        
        return response;
    }

    public async Task<CategoryUpdateResult> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return CategoryUpdateResult.NotFound;
        
        if (request.Name is not null)
            category.Name = request.Name;
        if (request.ClearColor)
            category.Color = null;
        else if (request.Color is not null)
            category.Color = request.Color;
         
        db.Categories.Update(category);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return CategoryUpdateResult.Conflict;
        }
        
        return CategoryUpdateResult.Updated;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null) return false;
         
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        
        return true;
    }
}
