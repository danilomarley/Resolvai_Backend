using Resolvai.Domain.Enums;

namespace Resolvai.Application.DTOs.Home;

public sealed record HomeSummaryResponse(
    OrderCountsResponse Orders,
    IReadOnlyList<RecentOrderResponse> RecentOrders);

public sealed record OrderCountsResponse(
    long Total, long Pending, long InProgress, long Completed, long Cancelled);

public sealed record RecentOrderResponse(
    Guid Id, string Title, OrderStatus Status, DateTime CreatedAt, DateTime? UpdatedAt);
