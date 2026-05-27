using Gridify;
using SubManager.Api.Application.Features.Categories.Models;

namespace SubManager.Api.Application.Features.Categories.Interfaces;

public interface ICategoryService
{
    Task<Paging<CategoryResponse>> GetAllAsync(GridifyQuery query, CancellationToken ct);
    Task<CategoryResponse?> GetAsync(int id, CancellationToken ct);
    Task<CategoryResponse?> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}