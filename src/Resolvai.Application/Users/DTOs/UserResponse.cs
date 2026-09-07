namespace Resolvai.Application.Users.DTOs;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt
);
