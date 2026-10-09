using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Users;
using Resolvai.Application.Mappings;
using Resolvai.Application.Services.Interfaces;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Application.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IEnderecoRepository enderecoRepository,
    IContatoRepository contatoRepository,
    ISupabaseAuthClient supabaseAuthClient,
    ICurrentUser currentUser) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = Email.Create(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException($"Já existe um usuário com o e-mail '{email.Value}'.");
        }

        var authUser = await supabaseAuthClient.AdminCreateUserAsync(
            email.Value,
            request.Password,
            request.Name,
            cancellationToken);

        var user = User.Register(authUser.Id, request.Name, email, request.Role);
        await userRepository.AddAsync(user, cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Usuário", id);

        return user.ToResponse();
    }

    public Task<UserResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var id = currentUser.Id ?? throw new UnauthorizedException("Token sem identificação de usuário.");

        return GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        return users.Select(user => user.ToResponse()).ToList();
    }

    public async Task<UserResponse> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Usuário", id);

        if (isActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await userRepository.UpdateAsync(user, cancellationToken);

        return user.ToResponse();
    }

    public async Task<UserResponse> CompleteRegistrationAsync(
        CompleteRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = currentUser.Id ?? throw new UnauthorizedException("Token sem identificação de usuário.");

        var user = await userRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            var email = Email.Create(currentUser.Email);
            user = User.Register(id, request.Name, email, UserRole.Cliente);
            await userRepository.AddAsync(user, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(user.Cpf))
        {
            throw new ConflictException("Cadastro já finalizado.");
        }

        var cpf = OnlyDigits(request.Cpf);

        if (await userRepository.ExistsByCpfAsync(cpf, id, cancellationToken))
        {
            throw new ConflictException($"Já existe um usuário com o CPF '{request.Cpf}'.");
        }

        user.CompleteRegistration(cpf);
        await userRepository.UpdateAsync(user, cancellationToken);

        var endereco = Endereco.Register(
            user.Id,
            request.Endereco.Logradouro,
            request.Endereco.Numero,
            request.Endereco.Complemento,
            request.Endereco.Bairro,
            request.Endereco.Cidade,
            request.Endereco.Estado.ToUpperInvariant(),
            OnlyDigits(request.Endereco.Cep),
            request.Endereco.Apelido,
            request.Endereco.Latitude,
            request.Endereco.Longitude,
            principal: true);
        await enderecoRepository.AddAsync(endereco, cancellationToken);

        var contato = Contato.Register(user.Id, request.Contato.Tipo, request.Contato.Valor, principal: true);
        await contatoRepository.AddAsync(contato, cancellationToken);

        return user.ToResponse();
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());
}
