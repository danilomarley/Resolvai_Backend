namespace Resolvai.Application.Users.DTOs;

public sealed record SetUserStatusRequest
{
    public required bool IsActive { get; init; }
}
