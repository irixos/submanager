namespace SubManager.Api.Application.Features.Channels.Models;

public sealed class ImportChannelsResponse
{
    public int CandidatesFound { get; init; }
    public int ImportedCount { get; init; }
    public int DuplicateCount { get; init; }
    public int FailedCount { get; init; }
    
    public IReadOnlyCollection<ChannelResponse> ImportedChannels { get; init; } = [];
}