using FluentValidation;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.Users.DTOs;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Application.Users;

public sealed class UserService(
    IUserRepository userRepository,
    ISupabaseAuthClient supabaseAuthClient,
    ICurrentUser currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<User> userValidator
) : IUserService
{
    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var email = Email.Create(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException($"Já existe um usuário com o e-mail '{email.Value}'.");
        }

        var authUser = await supabaseAuthClient.AdminCreateUserAsync(
            email.Value,
            request.Password,
            request.Name,
            cancellationToken
        );

        var user = User.Register(authUser.Id, request.Name, email, request.Role);
        await userValidator.ValidateAndThrowAsync(user, cancellationToken);
        await userRepository.AddAsync(user, cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var user =
            await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Usuário", id);

        return user.ToResponse();
    }

    public Task<UserResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var id =
            currentUser.Id
            ?? throw new UnauthorizedException("Token sem identificação de usuário.");

        return GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users.Select(user => user.ToResponse()).ToList();
    }

    public async Task<UserResponse> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default
    )
    {
        var user =
            await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Usuário", id);

        if (isActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await userValidator.ValidateAndThrowAsync(user, cancellationToken);
        await userRepository.UpdateAsync(user, cancellationToken);

        return user.ToResponse();
    }
}
