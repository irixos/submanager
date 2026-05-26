namespace SubManagerLite.Application.Entities;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hex color code for UI display (e.g., #FF5733).
    /// Optional - can be assigned by application if not provided.
    /// </summary>
    public string? Color { get; set; }

    // Navigation properties
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
}
