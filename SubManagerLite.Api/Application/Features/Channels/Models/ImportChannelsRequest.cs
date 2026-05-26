using System.ComponentModel.DataAnnotations;

namespace SubManagerLite.Application.Features.Channels.Models;

public sealed class ImportChannelsRequest
{
    private const int MaxFileSize = 5 * 1024 * 1024;
    [Required] 
    [MaxFileSize(MaxFileSize, ErrorMessage = "File size must be less than 5MB")]
    [AllowedExtensions(".txt", ".csv", ".json")]
    public IFormFile File { get; init; } = null!;
}
