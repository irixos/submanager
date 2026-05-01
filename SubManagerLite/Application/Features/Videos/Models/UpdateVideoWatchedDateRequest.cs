namespace SubManagerLite.Application.Features.Videos.Models;

public sealed class UpdateVideoWatchedDateRequest
{
    public DateTimeOffset? WatchedDate { get; init; }
}
