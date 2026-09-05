using System.ComponentModel.DataAnnotations;

namespace Resolvai.Application.DTOs.Auth;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public required string Email { get; init; }

    [Required]
    [MaxLength(128)]
    public required string Password { get; init; }
}
