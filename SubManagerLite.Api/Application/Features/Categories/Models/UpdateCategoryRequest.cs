using System.ComponentModel.DataAnnotations;

namespace SubManagerLite.Application.Features.Categories.Models;

public sealed class UpdateCategoryRequest : IValidatableObject
{
    [MaxLength(50)]
    [RegularExpression("^(?!\\s*$).+")]
    public string? Name { get; init; }

    [MaxLength(7)]
    [RegularExpression("^#([0-9A-Fa-f]{6})$")]
    public string? Color { get; init; }

    public bool ClearColor { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ClearColor && Color is not null)
        {
            yield return new ValidationResult(
                "Color must be null when ClearColor is true.",
                [nameof(Color), nameof(ClearColor)]);
        }
    }
}
