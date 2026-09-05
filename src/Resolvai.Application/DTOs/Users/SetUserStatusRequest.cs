namespace Resolvai.Application.DTOs.Users;

public sealed record SetUserStatusRequest
{
    public required bool IsActive { get; init; }
}
