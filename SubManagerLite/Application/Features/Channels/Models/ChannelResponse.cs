using SubManagerLite.Application.Features.Categories.Models;

namespace SubManagerLite.Application.Features.Channels.Models;

public sealed class ChannelResponse
{
    public int Id { get; init; }
    public string YoutubeChannelId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public DateTimeOffset AddedDate { get; init; }
    public DateTimeOffset? LastCheckedDate { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyCollection<CategoryResponse> Categories { get; init; } = [];
}
