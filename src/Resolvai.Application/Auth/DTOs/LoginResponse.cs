using Resolvai.Application.Users.DTOs;

namespace Resolvai.Application.Auth.DTOs;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    string? RefreshToken,
    UserResponse User
);
