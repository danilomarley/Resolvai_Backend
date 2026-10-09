using Resolvai.Application.DTOs.Pedidos;

namespace Resolvai.Application.DTOs.Home;

public sealed record ResumoHomeResponse(
    ContagemPedidosResponse Orders,
    ContagemPropostasResponse Proposals,
    IReadOnlyList<PedidoResumoResponse> RecentOrders);

/// <summary>
/// Active = Pending + InProgress. CreatedLast7Days usa o relógio do banco.
/// </summary>
public sealed record ContagemPedidosResponse(
    long Total, long Pending, long InProgress, long Completed, long Cancelled, long Active, long CreatedLast7Days);

/// <summary>
/// AwaitingDecision considera propostas de pedidos Pending; Unviewed, as dessas que ainda estão enviadas.
/// </summary>
public sealed record ContagemPropostasResponse(long Total, long AwaitingDecision, long Unviewed);
