using Dapper;
using Resolvai.Application.Contracts.Fornecedores;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class EspecialidadeQueries(IDbConnectionFactory connectionFactory) : IEspecialidadeQueries
{
    public async Task<IReadOnlyList<EspecialidadeResponse>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select codigo as "Codigo",
                   nome   as "Nome"
              from public.especialidades
             where is_active = true
             order by nome, codigo;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<EspecialidadeResponse>(
            new CommandDefinition(
                sql,
                commandTimeout: connectionFactory.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }
}
