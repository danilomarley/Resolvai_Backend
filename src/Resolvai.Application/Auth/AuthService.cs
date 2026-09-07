using FluentValidation;
using Resolvai.Application.Auth.DTOs;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.Users;
using Resolvai.Application.Users.DTOs;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Application.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    ISupabaseAuthClient supabaseAuthClient,
    IValidator<LoginRequest> loginValidator,
    IValidator<RegisterRequest> registerValidator,
    IValidator<User> userValidator
) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var email = Email.Create(request.Email);

        var session = await supabaseAuthClient.SignInWithPasswordAsync(
            email.Value,
            request.Password,
            cancellationToken
        );

        var user =
            await userRepository.GetByIdAsync(session.User.Id, cancellationToken)
            ?? await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException(
                user is null
                    ? "Usuário autenticado no Supabase sem perfil local."
                    : "Usuário inativo."
            );
        }

        return new LoginResponse(
            session.AccessToken,
            session.TokenType,
            session.ExpiresAtUtc,
            session.RefreshToken,
            user.ToResponse()
        );
    }

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        var email = Email.Create(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException($"Já existe um usuário com o e-mail '{email.Value}'.");
        }

        var authUser = await supabaseAuthClient.SignUpAsync(
            email.Value,
            request.Password,
            request.Name,
            cancellationToken
        );

        var user = User.Register(authUser.Id, request.Name, email, UserRole.Viewer);
        await userValidator.ValidateAndThrowAsync(user, cancellationToken);
        await userRepository.AddAsync(user, cancellationToken);

        return user.ToResponse();
    }
}
