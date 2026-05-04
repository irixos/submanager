using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Videos.Models;

namespace SubManagerLite.Application.Interfaces;

public interface IVideoRepository
{
    Task<List<Video>> GetAllAsync(CancellationToken ct);
    Task<Video?> GetAsync(int id, CancellationToken ct);
    Task<List<Video>> GetByYoutubeVideoIdsAsync(List<string> youtubeVideoIds, CancellationToken ct);
    Task<List<string>> GetNewVideoIdsAsync(List<string> youtubeVideoIds, CancellationToken ct);
    Task AddAsync(Video video, CancellationToken ct);
    Task UpdateAsync(Video video, CancellationToken ct);
    Task UpsertRangeAsync(List<Video> videos, CancellationToken ct);
    Task UpdateMetadataAsync(Dictionary<string, YoutubeVideoInfo> pendingVideoInfos, CancellationToken ct);
}