using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Orders;

namespace Resolvai.Application.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HomeSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
