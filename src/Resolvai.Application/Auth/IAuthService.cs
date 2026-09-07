using Resolvai.Application.Auth.DTOs;
using Resolvai.Application.Users.DTOs;

namespace Resolvai.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );

    Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
}
