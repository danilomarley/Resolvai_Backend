using Resolvai.Application.DTOs.Auth;
using Resolvai.Application.DTOs.Users;

namespace Resolvai.Application.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
