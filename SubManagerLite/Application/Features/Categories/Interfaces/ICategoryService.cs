using SubManagerLite.Application.Features.Categories.Models;

namespace SubManagerLite.Application.Features.Categories.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAllAsync(CancellationToken ct);
    Task<CategoryResponse?> GetAsync(int id, CancellationToken ct);
    Task<CategoryResponse?> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}