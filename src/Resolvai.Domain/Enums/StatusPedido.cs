namespace Resolvai.Domain.Enums;

/// <summary>
/// Persistido como texto na coluna pedidos.status (aberto/em_andamento/concluido/cancelado).
/// Os membros ficam em inglês porque são o valor exposto no JSON da API e já consumido pelo front.
/// </summary>
public enum StatusPedido
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}
