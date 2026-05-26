using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Features.Videos.Models;

public sealed class RefreshResult
{
    public IReadOnlyList<VideoResponse> Response { get; set; } = [];
    public bool IsAlreadyRunning { get; set;}
}
