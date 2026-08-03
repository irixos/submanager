using System.ComponentModel.DataAnnotations;

namespace SubManager.Api.Application.Features.Identity.Models;

/// <summary>Permanently deletes the authenticated user's account and application data.</summary>
public sealed record DeleteAccountRequest
{
    [Required]
    public string Password { get; init; } = string.Empty;
}
