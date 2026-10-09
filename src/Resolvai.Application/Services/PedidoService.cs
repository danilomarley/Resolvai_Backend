using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Pedidos;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Application.Services;

public sealed class PedidoService(IPedidoQueries pedidos, ICurrentUser currentUser) : IPedidoService
{
    // Os limites de página são validados no binding da rota (DataAnnotations de ListarPedidosQuery).
    public Task<PaginaResponse<PedidoResumoResponse>> ListAsync(ListarPedidosQuery query, CancellationToken cancellationToken = default)
        => pedidos.ListAsync(GetUserId(), query.Status, query.Page, query.PageSize, cancellationToken);

    public async Task<PedidoResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await pedidos.GetByIdAsync(id, GetUserId(), cancellationToken)
            ?? throw new NotFoundException("Pedido", id);

    public Task<ResumoHomeResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        => pedidos.GetSummaryAsync(GetUserId(), cancellationToken);

    private Guid GetUserId()
        => currentUser.IsAuthenticated && currentUser.Id is Guid id && id != Guid.Empty
            ? id
            : throw new UnauthorizedException("Token sem identificação de usuário.");
}
