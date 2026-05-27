namespace SubManager.Api.Application.Features.Channels.Models;

public sealed record YoutubeChannelRef(
    YoutubeChannelRefKind Kind,
    string Url);