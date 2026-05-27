namespace SubManager.Api.Application.Features.Channels.Models;

public sealed record YoutubeChannelInfo
{
    public string YoutubeChannelId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }
}
