using Resolvai.Application.Users.DTOs;

namespace Resolvai.Application.Users;

public interface IUserService
{
    Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default
    );

    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserResponse> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserResponse> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default
    );
}
