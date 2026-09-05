using System.ComponentModel.DataAnnotations;

namespace Resolvai.Application.DTOs.Auth;

/// <summary>
/// Auto-cadastro público. O papel é sempre Viewer; promoção só por um Admin.
/// </summary>
public sealed record RegisterRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public required string Email { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public required string Password { get; init; }
}
