using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Domain.Enums;

namespace Resolvai.Application.Contracts.Pedidos;

// Consultas de leitura retornam projeções, sem carregar agregados desnecessários.
public interface IPedidoQueries
{
    Task<PaginaResponse<PedidoResumoResponse>> ListAsync(
        Guid userId, StatusPedido? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PedidoResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ResumoHomeResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
