using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Orders;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Orders;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Application.Services;

public sealed class OrderService(IOrderQueries orders, ICurrentUser currentUser) : IOrderService
{
    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await orders.GetByIdAsync(id, GetUserId(), cancellationToken)
            ?? throw new NotFoundException("Pedido", id);

    public Task<HomeSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        => orders.GetSummaryAsync(GetUserId(), cancellationToken);

    private Guid GetUserId()
        => currentUser.IsAuthenticated && currentUser.Id is Guid id && id != Guid.Empty
            ? id
            : throw new UnauthorizedException("Token sem identificação de usuário.");
}
