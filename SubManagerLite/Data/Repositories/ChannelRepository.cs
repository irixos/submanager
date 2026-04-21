using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;

namespace SubManagerLite.Data.Repositories;

public sealed class ChannelRepository(ApplicationDbContext db)
{
    public Task<List<Channel>> GetAllAsync(CancellationToken ct)
    {
        return db.Channels.ToListAsync(ct);
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
}