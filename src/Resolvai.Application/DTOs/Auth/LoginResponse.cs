using Resolvai.Application.DTOs.Users;

namespace Resolvai.Application.DTOs.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    string? RefreshToken,
    UserResponse User);
