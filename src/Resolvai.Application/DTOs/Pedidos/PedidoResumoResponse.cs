using Resolvai.Domain.Enums;

namespace Resolvai.Application.DTOs.Pedidos;

/// <summary>
/// Item de listagem: usado em Seus Pedidos e nos pedidos recentes da Home.
/// </summary>
public sealed record PedidoResumoResponse(
    Guid Id,
    string Title,
    string? Category,
    StatusPedido Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    long ProposalsCount,
    decimal? ChosenProposalValue);
