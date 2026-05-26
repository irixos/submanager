namespace SubManagerLite.Application.Features.Categories.Models;

public sealed class CategoryResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
}
