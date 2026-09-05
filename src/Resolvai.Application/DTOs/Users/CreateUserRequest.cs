using System.ComponentModel.DataAnnotations;
using Resolvai.Domain.Enums;

namespace Resolvai.Application.DTOs.Users;

public sealed record CreateUserRequest
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

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; } = UserRole.Viewer;
}
