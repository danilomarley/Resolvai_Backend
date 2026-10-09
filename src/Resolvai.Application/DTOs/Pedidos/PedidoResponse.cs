using System.Text.Json;
using Resolvai.Domain.Enums;

namespace Resolvai.Application.DTOs.Pedidos;

public sealed record PedidoResponse(
    Guid Id,
    string Title,
    string? Category,
    string Description,
    string? Location,
    StatusPedido Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    JsonElement? Scope,
    IReadOnlyList<FotoPedidoResponse> Photos,
    IReadOnlyList<PropostaResponse> Proposals);

public sealed record FotoPedidoResponse(Guid Id, string Url, string? Description);

public sealed record PropostaResponse(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    StatusProposta Status,
    decimal Value,
    string? Deadline,
    string? Description,
    string? Warranty);
