using System.ComponentModel.DataAnnotations;

namespace SubManager.Api.Application;

public class MaxFileSizeAttribute(long maxFileSize) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file) return ValidationResult.Success;
        return file.Length > maxFileSize ? new ValidationResult(ErrorMessageString) : ValidationResult.Success;
    }

    public override string FormatErrorMessage(string name)
    {
        var maxMb = maxFileSize / 1024 / 1024;
        return $"The file '{name}' must be less than {maxMb}MB.";
    }
}