using SubManager.Api.Application.Features.Categories.Models;

namespace SubManager.Api.Application.Features.Videos.Models;

public sealed class VideoResponse
{
    public int Id { get; init; }
    public string YoutubeVideoId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public DateTimeOffset PublishedDate { get; init; }
    public DateTimeOffset AddedDate { get; init; }
    public DateTimeOffset MetadataLastRefreshedAt { get; init; }
    public DateTimeOffset? WatchedDate { get; init; }
    public int? DurationSeconds { get; init; }
    public long? ViewCount { get; init; }
    public bool IsWatched { get; init; }

    public VideoChannelResponse Channel { get; init; } = new();

    public IReadOnlyCollection<CategoryResponse> Categories { get; init; } = [];

    public sealed class VideoChannelResponse
    {
        public int Id { get; init; }
        public string YoutubeChannelId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? ThumbnailUrl { get; init; }
    }
}
