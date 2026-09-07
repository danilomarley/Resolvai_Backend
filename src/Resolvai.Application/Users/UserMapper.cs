using Resolvai.Application.Users.DTOs;
using Resolvai.Domain.Entities;

namespace Resolvai.Application.Users;

public static class UserMapper
{
    public static UserResponse ToResponse(this User user) =>
        new(
            user.Id,
            user.Name,
            user.Email.Value,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        );
}
