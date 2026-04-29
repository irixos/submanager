using SubManagerLite.Application.Features.Channels.Models;

namespace SubManagerLite.Application.Features.Channels.Interfaces;

public interface IChannelService
{
    Task<List<ChannelResponse>> GetAllAsync(CancellationToken ct);
    Task<ChannelResponse?> GetAsync(int id, CancellationToken ct);
    Task<ChannelResponse?> CreateAsync(CreateChannelRequest request, CancellationToken ct);
    Task<bool> UpdateCategoriesAsync(int id, UpdateChannelCategoriesRequest request, CancellationToken ct);
    Task<bool> UpdateStatusAsync(int id, UpdateChannelStatusRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}