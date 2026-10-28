using System.ComponentModel.DataAnnotations;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Fornecedores;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Application.Mappings;
using Resolvai.Application.Services.Interfaces;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Exceptions;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Application.Services;

public sealed class FornecedorService(
    IUserRepository userRepository,
    IFornecedorRepository fornecedorRepository,
    IEspecialidadeQueries especialidadeQueries,
    ISupabaseAuthClient supabaseAuthClient,
    ICurrentUser currentUser) : IFornecedorService
{
    public async Task<FornecedorResponse> RegisterAsync(
        RegisterFornecedorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Validator.TryValidateObject(request, new ValidationContext(request), null, validateAllProperties: true))
        {
            throw new DomainException("Dados de cadastro inválidos. Confira os campos obrigatórios e seus limites.");
        }

        if (!Email.TryCreate(request.Email, out var email))
        {
            throw new DomainException("E-mail inválido.");
        }
        var tipoPessoa = Enum.Parse<TipoPessoa>(request.TipoPessoa);
        var provisionalId = Guid.NewGuid();
        // Valida as entidades antes de qualquer efeito externo.
        _ = User.Register(provisionalId, request.Name, email, UserRole.Prestador);
        var profile = Fornecedor.Register(
            provisionalId, tipoPessoa, request.NomeExibicao, request.Cnpj,
            request.CidadeAtuacao, request.UfAtuacao, request.RaioAtendimentoKm,
            request.Bio, request.RegistroProfissional);

        var cpf = profile.NormalizeCpf(request.Cpf);
        var telefone = Telefone.Create(request.Telefone).Value;

        var codigos = request.Especialidades.Select(c => c?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (codigos.Length == 0 || codigos.Any(c => string.IsNullOrWhiteSpace(c) || c.Length > 100))
        {
            throw new DomainException("Selecione ao menos uma especialidade válida.");
        }

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException("E-mail já cadastrado. Use uma conta distinta para cada papel.");
        }

        if (cpf is not null
            && await userRepository.ExistsByCpfAsync(cpf, UserRole.Prestador, Guid.Empty, cancellationToken))
        {
            throw new ConflictException("Já existe prestador com este CPF.");
        }

        if (profile.Cnpj is not null
            && await fornecedorRepository.ExistsByCnpjAsync(profile.Cnpj, cancellationToken))
        {
            throw new ConflictException("CNPJ já cadastrado.");
        }

        var permitidas = (await especialidadeQueries.ListActiveAsync(cancellationToken))
            .Select(c => c.Codigo).ToHashSet(StringComparer.Ordinal);
        if (codigos.Any(c => !permitidas.Contains(c)))
        {
            throw new DomainException("Há especialidades inexistentes ou inativas.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Depois do início do signup, a desconexão HTTP não deve abandonar o perfil.
        // Auth e PostgreSQL continuam separados; o limite interno evita execução sem fim.
        using var registration = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        SupabaseAuthUser authUser;
        try
        {
            authUser = await supabaseAuthClient.SignUpAsync(
                email.Value, request.Password, request.Name.Trim(), registration.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            throw new RegistrationPendingException();
        }

        if (authUser.Id == Guid.Empty || !string.Equals(authUser.Email, email.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("Não foi possível confirmar a identidade da conta.");
        }

        var user = User.Register(authUser.Id, request.Name, email, UserRole.Prestador);
        if (cpf is not null)
        {
            user.CompleteRegistration(cpf);
        }

        var fornecedor = Fornecedor.Register(
            user.Id, tipoPessoa, request.NomeExibicao, profile.Cnpj,
            request.CidadeAtuacao, request.UfAtuacao, request.RaioAtendimentoKm,
            request.Bio, request.RegistroProfissional);
        var contato = Contato.Register(user.Id, "whatsapp", telefone, principal: true);

        try
        {
            await fornecedorRepository.AddCompleteAsync(user, fornecedor, contato, codigos, registration.Token);
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception)
        {
            // Não propaga dados do driver nem apaga automaticamente contas externas.
            // O signup recuperável permite reapresentar os mesmos dados após confirmar o e-mail.
            throw new RegistrationPendingException();
        }

        return fornecedor.ToResponse(codigos);
    }

    public async Task<FornecedorResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.Role != UserRole.Prestador
            || currentUser.Id is not Guid userId || userId == Guid.Empty)
        {
            throw new UnauthorizedException("É necessário estar autenticado como Prestador.");
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || user.Role != UserRole.Prestador)
        {
            throw new UnauthorizedException("Perfil de Prestador indisponível.");
        }

        var fornecedor = await fornecedorRepository.GetByUsuarioIdAsync(userId, cancellationToken);
        if (fornecedor is null || fornecedor.IdUsuario != userId)
        {
            throw new NotFoundException("Fornecedor", userId);
        }

        var especialidades = await fornecedorRepository.GetEspecialidadesAsync(userId, cancellationToken);
        return fornecedor.ToResponse(especialidades);
    }
}
