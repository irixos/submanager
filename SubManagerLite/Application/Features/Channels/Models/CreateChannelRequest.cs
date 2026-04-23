using System.ComponentModel.DataAnnotations;

namespace SubManagerLite.Application.Features.Channels.Models;

public sealed class CreateChannelRequest
{
    [Required]
    public string ChannelUrl { get; init; } = string.Empty;

    public List<int>? CategoryIds { get; init; }
}