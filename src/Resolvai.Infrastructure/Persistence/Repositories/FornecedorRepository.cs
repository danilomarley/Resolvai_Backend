using Dapper;
using Npgsql;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Exceptions;
using Resolvai.Domain.Repositories;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class FornecedorRepository(IDbConnectionFactory connectionFactory) : IFornecedorRepository
{
    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        const string sql = "select exists (select 1 from public.fornecedores where cnpj = @Cnpj);";

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { Cnpj = cnpj },
                commandTimeout: connectionFactory.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
    }

    public async Task AddCompleteAsync(
        User user,
        Fornecedor fornecedor,
        Contato contato,
        IReadOnlyList<string> especialidades,
        CancellationToken cancellationToken = default)
    {
        ValidateRegistration(user, fornecedor, contato, especialidades);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        CommandDefinition Command(string sql, object? parameters) => new(
            sql,
            parameters,
            transaction,
            connectionFactory.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        try
        {
            // Mantém as especialidades ativas e existentes até o fim da transação.
            var ativas = (await connection.QueryAsync<string>(Command(
                """
                select codigo
                  from public.especialidades
                 where is_active = true
                   and codigo = any(@Codigos)
                 order by codigo
                 for share;
                """,
                new { Codigos = especialidades.ToArray() }))).ToHashSet(StringComparer.Ordinal);

            if (especialidades.Any(codigo => !ativas.Contains(codigo)))
            {
                throw new DomainException("Há especialidades inexistentes ou inativas.");
            }

            await connection.ExecuteAsync(Command(
                """
                insert into public.users (id, name, email, role, is_active, created_at, updated_at, cpf)
                values (@Id, @Name, @Email, @Role, @IsActive, @CreatedAt, @UpdatedAt, @Cpf);
                """,
                new
                {
                    user.Id,
                    user.Name,
                    Email = user.Email.Value,
                    Role = user.Role.ToString(),
                    user.IsActive,
                    user.CreatedAt,
                    user.UpdatedAt,
                    user.Cpf
                }));

            await connection.ExecuteAsync(Command(
                """
                insert into public.fornecedores
                    (id_usuario, tipo_pessoa, nome_exibicao, cnpj, bio, cidade_atuacao,
                     uf_atuacao, raio_atendimento_km, registro_profissional, created_at, updated_at)
                values
                    (@IdUsuario, @TipoPessoa, @NomeExibicao, @Cnpj, @Bio, @CidadeAtuacao,
                     @UfAtuacao, @RaioAtendimentoKm, @RegistroProfissional, @CreatedAt, @UpdatedAt);
                """,
                new
                {
                    fornecedor.IdUsuario,
                    TipoPessoa = fornecedor.TipoPessoa.ToString(),
                    fornecedor.NomeExibicao,
                    fornecedor.Cnpj,
                    fornecedor.Bio,
                    fornecedor.CidadeAtuacao,
                    fornecedor.UfAtuacao,
                    fornecedor.RaioAtendimentoKm,
                    fornecedor.RegistroProfissional,
                    fornecedor.CreatedAt,
                    fornecedor.UpdatedAt
                }));

            await connection.ExecuteAsync(Command(
                """
                insert into public.contatos (id, id_usuario, tipo, valor, principal)
                values (@Id, @IdUsuario, @Tipo, @Valor, @Principal);
                """,
                new { contato.Id, contato.IdUsuario, contato.Tipo, contato.Valor, contato.Principal }));

            foreach (var codigo in especialidades)
            {
                await connection.ExecuteAsync(Command(
                    """
                    insert into public.fornecedor_especialidades (id_usuario, codigo_especialidade)
                    values (@IdUsuario, @Codigo);
                    """,
                    new { fornecedor.IdUsuario, Codigo = codigo }));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // O descarte da transação faz rollback de todas as tabelas, inclusive em cancelamento.
            throw new ConflictException("E-mail, documento ou perfil já cadastrado.");
        }
    }

    public async Task<Fornecedor?> GetByUsuarioIdAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id_usuario            as "IdUsuario",
                   tipo_pessoa           as "TipoPessoa",
                   nome_exibicao         as "NomeExibicao",
                   cnpj                  as "Cnpj",
                   bio                   as "Bio",
                   cidade_atuacao        as "CidadeAtuacao",
                   uf_atuacao            as "UfAtuacao",
                   raio_atendimento_km   as "RaioAtendimentoKm",
                   registro_profissional as "RegistroProfissional",
                   created_at            as "CreatedAt",
                   updated_at            as "UpdatedAt"
              from public.fornecedores
             where id_usuario = @IdUsuario;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<FornecedorRow>(
            new CommandDefinition(
                sql,
                new { IdUsuario = idUsuario },
                commandTimeout: connectionFactory.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<string>> GetEspecialidadesAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select codigo_especialidade
              from public.fornecedor_especialidades
             where id_usuario = @IdUsuario
             order by codigo_especialidade;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new { IdUsuario = idUsuario },
                commandTimeout: connectionFactory.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    private static void ValidateRegistration(
        User user,
        Fornecedor fornecedor,
        Contato contato,
        IReadOnlyList<string> especialidades)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(fornecedor);
        ArgumentNullException.ThrowIfNull(contato);
        ArgumentNullException.ThrowIfNull(especialidades);

        if (user.Id == Guid.Empty || user.Role != UserRole.Prestador
            || fornecedor.IdUsuario != user.Id || contato.IdUsuario != user.Id)
        {
            throw new DomainException("Usuário, fornecedor e contato devem pertencer ao mesmo Prestador.");
        }

        fornecedor.ValidateRegistration(user, contato);

        if (especialidades.Count == 0 || especialidades.Any(string.IsNullOrWhiteSpace)
            || especialidades.Distinct(StringComparer.Ordinal).Count() != especialidades.Count)
        {
            throw new DomainException("Informe ao menos uma especialidade, sem repetições.");
        }
    }

    private sealed record FornecedorRow(
        Guid IdUsuario,
        string TipoPessoa,
        string NomeExibicao,
        string? Cnpj,
        string? Bio,
        string CidadeAtuacao,
        string UfAtuacao,
        int RaioAtendimentoKm,
        string? RegistroProfissional,
        DateTime CreatedAt,
        DateTime? UpdatedAt)
    {
        public Fornecedor ToDomain() => Fornecedor.Restore(
            IdUsuario,
            Enum.Parse<TipoPessoa>(TipoPessoa),
            NomeExibicao,
            Cnpj,
            Bio,
            CidadeAtuacao,
            UfAtuacao,
            RaioAtendimentoKm,
            RegistroProfissional,
            CreatedAt,
            UpdatedAt);
    }
}
