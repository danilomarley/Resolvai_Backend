using Resolvai.Domain.Enums;

namespace Resolvai.Application.DTOs.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Title,
    string Description,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
