using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Infrastructure.Repositories;

public class VideoRepository(ApplicationDbContext db) : IVideoRepository
{
    public Task<List<Video>> GetAllAsync(CancellationToken ct)
    {
        return db.Videos
            .Include(v => v.Channel)
            .ThenInclude(c => c.Categories)
            .ToListAsync(ct);
    }

    public async Task<Video?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Videos
            .Include(v => v.Channel)
            .ThenInclude(c => c.Categories)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
    
    public Task AddAsync(Video video, CancellationToken ct)
    {
        db.Videos.Add(video);
        return db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Video video, CancellationToken ct)
    {
        db.Videos.Update(video);
        return db.SaveChangesAsync(ct);
    }

}