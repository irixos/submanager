namespace SubManagerLite.Entities;

public class Video
{
    public int Id { get; set; }

    /// <summary>
    /// YouTube's unique video identifier (e.g., dQw4w9WgXcQ)
    /// </summary>
    public string YoutubeVideoId { get; set; } = string.Empty;

    public int ChannelId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    public DateTimeOffset PublishedDate { get; set; }

    public DateTimeOffset AddedDate { get; set; }

    /// <summary>
    /// Last time metadata fields were refreshed from YouTube.
    /// </summary>
    public DateTimeOffset MetadataLastRefreshedAt { get; set; }

    /// <summary>
    /// When the video was marked as watched. Null = unwatched
    /// </summary>
    public DateTimeOffset? WatchedDate { get; set; }

    public int? DurationSeconds { get; set; }

    public long? ViewCount { get; set; }

    /// <summary>
    /// Computed property indicating if the video has been watched
    /// </summary>
    public bool IsWatched => WatchedDate.HasValue;

    // Navigation properties
    public Channel Channel { get; set; } = null!;
}
