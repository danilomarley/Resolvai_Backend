using Dapper;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Repositories;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class ContatoRepository(IDbConnectionFactory connectionFactory) : IContatoRepository
{
    public async Task AddAsync(Contato contato, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into contatos (id, id_usuario, tipo, valor, principal)
            values (@Id, @IdUsuario, @Tipo, @Valor, @Principal);
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            contato.Id,
            contato.IdUsuario,
            contato.Tipo,
            contato.Valor,
            contato.Principal
        };

        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, commandTimeout: connectionFactory.CommandTimeoutSeconds, cancellationToken: cancellationToken));
    }
}
