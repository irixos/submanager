namespace SubManager.Api.Application.Features.Videos.Models;

/// <summary>
/// Represents the currently available duration for a video.
/// </summary>
public sealed class VideoDurationResponse
{
    public int Id { get; init; }
    public int? DurationSeconds { get; init; }
}
