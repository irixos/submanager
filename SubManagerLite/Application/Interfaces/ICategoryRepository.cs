using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken ct);
    Task<Category?> GetAsync(int id, CancellationToken ct);
    Task<List<Category>> GetByIdsAsync(List<int> ids, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task DeleteAsync(Category category, CancellationToken ct);
}