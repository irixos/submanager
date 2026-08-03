using System.ComponentModel.DataAnnotations;

namespace SubManager.Api.Application.Features.Identity.Models;

/// <summary>Changes the authenticated user's login email.</summary>
public sealed record ChangeEmailRequest
{
    [Required]
    [EmailAddress]
    public string NewEmail { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
