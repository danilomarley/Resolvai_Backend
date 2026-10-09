using System.Data;
using Dapper;
using Resolvai.Application.Contracts.Orders;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Orders;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class OrderQueries(IDbConnectionFactory connectionFactory) : IOrderQueries
{
    public async Task<OrderResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id as "Id", title as "Title", description as "Description",
                   status as "Status", created_at as "CreatedAt", updated_at as "UpdatedAt"
              from public.orders
             where id = @Id and user_id = @UserId;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<OrderResponse>(
            new CommandDefinition(sql, new { Id = id, UserId = userId },
                commandTimeout: connectionFactory.CommandTimeoutSeconds, cancellationToken: cancellationToken));
    }

    public async Task<HomeSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select count(*) as "Total",
                   count(*) filter (where status = 'Pending') as "Pending",
                   count(*) filter (where status = 'InProgress') as "InProgress",
                   count(*) filter (where status = 'Completed') as "Completed",
                   count(*) filter (where status = 'Cancelled') as "Cancelled"
              from public.orders
             where user_id = @UserId;

            select id as "Id", title as "Title", status as "Status",
                   created_at as "CreatedAt", updated_at as "UpdatedAt"
              from public.orders
             where user_id = @UserId
             order by created_at desc, id desc
             limit 5;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        // Mantém os contadores e a lista consistentes mesmo com alterações concorrentes.
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        HomeSummaryResponse response;
        using (var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId }, transaction,
                commandTimeout: connectionFactory.CommandTimeoutSeconds, cancellationToken: cancellationToken)))
        {
            var counts = await results.ReadSingleAsync<OrderCountsResponse>();
            var recent = (await results.ReadAsync<RecentOrderResponse>()).ToList();
            response = new HomeSummaryResponse(counts, recent);
        }

        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
