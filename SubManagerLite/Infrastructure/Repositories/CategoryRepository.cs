using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Infrastructure.Repositories;

public sealed class CategoryRepository(ApplicationDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync(CancellationToken ct)
    {
        return db.Categories.ToListAsync(ct);
    }

    public async Task<Category?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Categories.FindAsync([id], ct);
    }

    public async Task<List<Category>> GetByIdsAsync(List<int> ids, CancellationToken ct)
    {
        return await db.Categories
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
    }
    
    public Task AddAsync(Category category, CancellationToken ct)
    {
        db.Categories.Add(category);
        return db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Category category, CancellationToken ct)
    {
        db.Categories.Update(category);
        return db.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(Category category, CancellationToken ct)
    {
        db.Categories.Remove(category);
        return db.SaveChangesAsync(ct);
    }
}