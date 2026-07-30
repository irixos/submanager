namespace SubManager.Api.Application.Features.Videos.Models;

/// <summary>
/// Represents duration metadata progress for requested videos.
/// </summary>
public sealed class VideoDurationStatusResponse
{
    public bool HasPendingMetadata { get; init; }
    public IReadOnlyCollection<VideoDurationResponse> Videos { get; init; } = [];
}
