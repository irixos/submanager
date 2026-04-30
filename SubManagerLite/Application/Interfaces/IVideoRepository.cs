using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Interfaces;

public interface IVideoRepository
{
    Task<List<Video>> GetAllAsync(CancellationToken ct);
    Task<Video?> GetAsync(int id, CancellationToken ct);
    Task AddAsync(Video video, CancellationToken ct);
    Task UpdateAsync(Video video, CancellationToken ct);
}