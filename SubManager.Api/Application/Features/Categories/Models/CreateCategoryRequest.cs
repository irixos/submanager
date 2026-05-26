using System.ComponentModel.DataAnnotations;

namespace SubManagerLite.Application.Features.Categories.Models;

public sealed class CreateCategoryRequest
{
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^(?!\\s*$).+")]
    [MaxLength(50)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(7)]
    [RegularExpression("^#([0-9A-Fa-f]{6})$")]
    public string? Color { get; init; }
}
