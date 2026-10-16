using Dapper;
using Resolvai.Application.Contracts.Pedidos;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class PedidoRepository(IDbConnectionFactory connectionFactory) : IPedidoRepository
{
    public async Task AddAsync(
        Guid id,
        Guid idCliente,
        string escopoJson,
        IReadOnlyList<NovaFotoPedidoRequest> fotos,
        CancellationToken cancellationToken = default)
    {
        const string insertPedido = """
            insert into public.pedidos (id, id_cliente, escopo, status)
            values (@Id, @IdCliente, cast(@Escopo as jsonb), 'aberto');
            """;

        const string insertFoto = """
            insert into public.fotos_pedido (id, id_pedido, foto, descricao)
            values (@Id, @IdPedido, @Foto, @Descricao);
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            insertPedido,
            new { Id = id, IdCliente = idCliente, Escopo = escopoJson },
            transaction,
            connectionFactory.CommandTimeoutSeconds,
            cancellationToken: cancellationToken));

        foreach (var foto in fotos)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                insertFoto,
                new { Id = Guid.NewGuid(), IdPedido = id, Foto = foto.Url, foto.Descricao },
                transaction,
                connectionFactory.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
