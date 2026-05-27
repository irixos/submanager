using SubManager.Api.Application.Entities;

namespace SubManager.Api.Application.Features.Videos.Models;

public sealed class RefreshResult
{
    public IReadOnlyList<VideoResponse> Response { get; set; } = [];
    public bool IsAlreadyRunning { get; set;}
}
