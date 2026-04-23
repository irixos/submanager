using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Interfaces;

public interface IChannelRepository
{
    Task<List<Channel>> GetAllAsync(CancellationToken ct);
    Task<Channel?> GetAsync(int id, CancellationToken ct);
    Task AddAsync(Channel channel, CancellationToken ct);
    Task UpdateAsync(Channel channel, CancellationToken ct);
    Task DeleteAsync(Channel channel, CancellationToken ct);
}