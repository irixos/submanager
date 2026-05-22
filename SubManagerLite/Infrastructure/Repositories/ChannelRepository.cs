using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Infrastructure.Repositories;

public sealed class ChannelRepository(ApplicationDbContext db) : IChannelRepository
{
    public Task<List<Channel>> GetAllAsync(CancellationToken ct)
    {
        return db.Channels
            .AsNoTracking()
            .Include(c => c.Categories)
            .ToListAsync(ct);
    }

    public Task<List<Channel>> GetAllActiveAsync(CancellationToken ct)
    {
        return db.Channels
            .Where(c => c.IsActive)
            .ToListAsync(ct);
    }

    public Task<Channel?> GetAsync(int id, CancellationToken ct)
    {
        return db.Channels
            .AsNoTracking()
            .Include(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
    
    public Task AddAsync(Channel channel, CancellationToken ct)
    {
        db.Channels.Add(channel);
        return db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Channel channel, CancellationToken ct)
    {
        db.Channels.Update(channel);
        return db.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(Channel channel, CancellationToken ct)
    {
        db.Channels.Remove(channel);
        return db.SaveChangesAsync(ct);
    }
    
    public Task SaveChangesAsync(CancellationToken ct)
    {
        return db.SaveChangesAsync(ct);
    }
}