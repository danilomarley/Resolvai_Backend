using Resolvai.Domain.Enums;

namespace Resolvai.Application.Users.DTOs;

public sealed record CreateUserRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public UserRole Role { get; init; } = UserRole.Viewer;
}
