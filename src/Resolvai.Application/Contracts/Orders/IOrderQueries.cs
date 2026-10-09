using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Orders;

namespace Resolvai.Application.Contracts.Orders;

// Consultas de leitura retornam projeções, sem carregar agregados desnecessários.
public interface IOrderQueries
{
    Task<OrderResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<HomeSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
