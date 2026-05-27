using System.ComponentModel.DataAnnotations;

namespace SubManager.Api.Application;

public sealed class AllowedExtensionsAttribute(params string[] extensions) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
            return ValidationResult.Success;

        var extension = Path.GetExtension(file.FileName);

        return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessageString);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The file '{name}' must have one of these extensions: {string.Join(", ", extensions)}.";
    }
}