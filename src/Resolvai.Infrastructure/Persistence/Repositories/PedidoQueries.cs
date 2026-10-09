using System.Data;
using System.Text.Json;
using Dapper;
using Resolvai.Application.Contracts.Pedidos;
using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Domain.Enums;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

/// <summary>
/// Lê public.pedidos, public.propostas e public.fotos_pedido. Título, categoria, descrição e
/// localização vêm do jsonb pedidos.escopo (chaves descritas em docs/ORDERS-AND-HOME.md).
/// </summary>
public sealed class PedidoQueries(IDbConnectionFactory connectionFactory) : IPedidoQueries
{
    // Os status do banco ficam em português; a API expõe os nomes de StatusPedido/StatusProposta.
    // Manter alinhado com ToDbStatus.
    private const string StatusPedidoColumn = """
        case p.status
                    when 'aberto' then 'Pending'
                    when 'em_andamento' then 'InProgress'
                    when 'concluido' then 'Completed'
                    when 'cancelado' then 'Cancelled'
               end
        """;

    private const string StatusPropostaColumn = """
        case pr.status
                    when 'enviada' then 'Sent'
                    when 'visualizada' then 'Viewed'
                    when 'escolhida' then 'Chosen'
               end
        """;

    private const string CategoriaColumn = "nullif(btrim(p.escopo ->> 'categoria'), '')";

    private const string TituloColumn = $"coalesce(nullif(btrim(p.escopo ->> 'titulo'), ''), {CategoriaColumn}, 'Pedido')";

    // A ordem das colunas acompanha o construtor de PedidoResumoResponse, exigência do Dapper.
    private const string ResumoColumns = $"""
        select p.id as "Id",
               {TituloColumn} as "Title",
               {CategoriaColumn} as "Category",
               {StatusPedidoColumn} as "Status",
               p.data_criacao as "CreatedAt",
               p.data_atualizacao as "UpdatedAt",
               (select count(*) from public.propostas pr where pr.id_pedido = p.id) as "ProposalsCount",
               (select max(pr.valor) from public.propostas pr
                 where pr.id_pedido = p.id and pr.status = 'escolhida') as "ChosenProposalValue"
          from public.pedidos p
        """;

    private const string OrdenacaoRecentes = "order by p.data_criacao desc, p.id desc";

    public Task<PaginaResponse<PedidoResumoResponse>> ListAsync(
        Guid userId, StatusPedido? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filtro = status is null ? string.Empty : " and p.status = @Status";
        var sql = $"""
            select count(*) from public.pedidos p where p.id_cliente = @UserId{filtro};

            {ResumoColumns}
             where p.id_cliente = @UserId{filtro}
             {OrdenacaoRecentes}
             limit @PageSize offset @Offset;
            """;

        var parameters = new
        {
            UserId = userId,
            Status = status is { } value ? ToDbStatus(value) : null,
            PageSize = pageSize,
            Offset = (page - 1L) * pageSize
        };

        return ReadSnapshotAsync(sql, parameters, async results =>
        {
            var total = await results.ReadSingleAsync<long>();
            var items = (await results.ReadAsync<PedidoResumoResponse>()).ToList();
            return PaginaResponse<PedidoResumoResponse>.Create(items, page, pageSize, total);
        }, cancellationToken);
    }

    public Task<PedidoResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            select p.id as "Id",
                   {TituloColumn} as "Title",
                   {CategoriaColumn} as "Category",
                   coalesce(btrim(p.escopo ->> 'descricao'), '') as "Description",
                   nullif(btrim(p.escopo ->> 'localizacao'), '') as "Location",
                   {StatusPedidoColumn} as "Status",
                   p.data_criacao as "CreatedAt",
                   p.data_atualizacao as "UpdatedAt",
                   p.escopo::text as "Scope"
              from public.pedidos p
             where p.id = @Id and p.id_cliente = @UserId;

            select f.id as "Id", f.foto as "Url", f.descricao as "Description"
              from public.fotos_pedido f
              join public.pedidos p on p.id = f.id_pedido
             where f.id_pedido = @Id and p.id_cliente = @UserId
             order by f.id;

            select pr.id as "Id",
                   pr.id_prestador as "ProviderId",
                   u.name as "ProviderName",
                   {StatusPropostaColumn} as "Status",
                   pr.valor as "Value",
                   pr.prazo as "Deadline",
                   pr.descricao as "Description",
                   pr.garantia as "Warranty"
              from public.propostas pr
              join public.pedidos p on p.id = pr.id_pedido
              join public.users u on u.id = pr.id_prestador
             where pr.id_pedido = @Id and p.id_cliente = @UserId
             order by pr.status = 'escolhida' desc, pr.valor, pr.id;
            """;

        return ReadSnapshotAsync<PedidoResponse?>(sql, new { Id = id, UserId = userId }, async results =>
        {
            var pedido = await results.ReadSingleOrDefaultAsync<PedidoRow>();
            if (pedido is null)
            {
                return null;
            }

            var fotos = (await results.ReadAsync<FotoPedidoResponse>()).ToList();
            var propostas = (await results.ReadAsync<PropostaResponse>()).ToList();
            return pedido.ToResponse(fotos, propostas);
        }, cancellationToken);
    }

    public Task<ResumoHomeResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            select count(*) as "Total",
                   count(*) filter (where status = 'aberto') as "Pending",
                   count(*) filter (where status = 'em_andamento') as "InProgress",
                   count(*) filter (where status = 'concluido') as "Completed",
                   count(*) filter (where status = 'cancelado') as "Cancelled",
                   count(*) filter (where status in ('aberto', 'em_andamento')) as "Active",
                   count(*) filter (where data_criacao >= now() - interval '7 days') as "CreatedLast7Days"
              from public.pedidos
             where id_cliente = @UserId;

            select count(*) as "Total",
                   count(*) filter (where p.status = 'aberto') as "AwaitingDecision",
                   count(*) filter (where p.status = 'aberto' and pr.status = 'enviada') as "Unviewed"
              from public.propostas pr
              join public.pedidos p on p.id = pr.id_pedido
             where p.id_cliente = @UserId;

            {ResumoColumns}
             where p.id_cliente = @UserId
             {OrdenacaoRecentes}
             limit 5;
            """;

        return ReadSnapshotAsync(sql, new { UserId = userId }, async results =>
        {
            var pedidos = await results.ReadSingleAsync<ContagemPedidosResponse>();
            var propostas = await results.ReadSingleAsync<ContagemPropostasResponse>();
            var recentes = (await results.ReadAsync<PedidoResumoResponse>()).ToList();
            return new ResumoHomeResponse(pedidos, propostas, recentes);
        }, cancellationToken);
    }

    /// <summary>
    /// Executa as consultas numa transação curta de leitura: contadores, página e listas relacionadas
    /// vêm do mesmo snapshot mesmo com alterações concorrentes.
    /// </summary>
    private async Task<T> ReadSnapshotAsync<T>(
        string sql, object parameters, Func<SqlMapper.GridReader, Task<T>> read, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        T result;
        using (var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, transaction,
                commandTimeout: connectionFactory.CommandTimeoutSeconds, cancellationToken: cancellationToken)))
        {
            result = await read(results);
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static string ToDbStatus(StatusPedido status) => status switch
    {
        StatusPedido.Pending => "aberto",
        StatusPedido.InProgress => "em_andamento",
        StatusPedido.Completed => "concluido",
        StatusPedido.Cancelled => "cancelado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status de pedido desconhecido.")
    };

    private sealed record PedidoRow(
        Guid Id,
        string Title,
        string? Category,
        string Description,
        string? Location,
        StatusPedido Status,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? Scope)
    {
        public PedidoResponse ToResponse(IReadOnlyList<FotoPedidoResponse> fotos, IReadOnlyList<PropostaResponse> propostas)
        {
            JsonElement? scope = Scope is null ? null : JsonSerializer.Deserialize<JsonElement>(Scope);
            if (scope?.ValueKind == JsonValueKind.Null)
            {
                scope = null;
            }

            return new PedidoResponse(
                Id, Title, Category, Description, Location, Status, CreatedAt, UpdatedAt, scope, fotos, propostas);
        }
    }
}
