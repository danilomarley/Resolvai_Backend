using System.ComponentModel.DataAnnotations;
using Resolvai.Domain.Enums;

namespace Resolvai.Application.DTOs.Pedidos;

/// <summary>
/// Parâmetros de query de GET /api/v1/orders. Os nomes seguem o contrato da API (status, page, pageSize).
/// </summary>
public sealed record ListarPedidosQuery
{
    public const int MaxPageSize = 50;

    /// <summary>
    /// Filtro opcional. Sem valor, lista pedidos de todos os status.
    /// </summary>
    [EnumDataType(typeof(StatusPedido))]
    public StatusPedido? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 10;
}
