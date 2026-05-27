namespace SubManager.Api.Application.Entities;

public class Channel
{
    public int Id { get; set; }

    /// <summary>
    /// YouTube's unique channel identifier (e.g., UCxxxxxx)
    /// </summary>
    public string YoutubeChannelId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    
    public string? ThumbnailUrl { get; set; }

    public DateTimeOffset AddedDate { get; set; }

    public DateTimeOffset? LastCheckedDate { get; set; }

    /// <summary>
    /// Whether to actively poll this channel for updates
    /// </summary>
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
