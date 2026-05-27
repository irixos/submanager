namespace SubManager.Api.Application.Features.Channels.Models;

public sealed class UpdateChannelCategoriesRequest
{
    public IReadOnlyCollection<int>? CategoryIds { get; init; }
}
