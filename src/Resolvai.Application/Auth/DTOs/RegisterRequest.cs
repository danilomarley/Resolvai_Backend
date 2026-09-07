namespace Resolvai.Application.Auth.DTOs;

/// <summary>
/// Auto-cadastro público. O papel é sempre Viewer; promoção só por um Admin.
/// </summary>
public sealed record RegisterRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
}
