using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Pedidos;

namespace Resolvai.Application.Services.Interfaces;

public interface IPedidoService
{
    Task<PaginaResponse<PedidoResumoResponse>> ListAsync(ListarPedidosQuery query, CancellationToken cancellationToken = default);
    Task<PedidoResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResumoHomeResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
