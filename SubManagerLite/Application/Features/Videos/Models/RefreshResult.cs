using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Features.Videos.Models;

public sealed class RefreshResult
{
    public List<VideoResponse> Response { get; set; } = [];
    public bool IsAlreadyRunning { get; set;}
}