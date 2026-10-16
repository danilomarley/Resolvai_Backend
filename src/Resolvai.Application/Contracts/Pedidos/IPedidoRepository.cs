using Resolvai.Application.DTOs.Pedidos;

namespace Resolvai.Application.Contracts.Pedidos;

public interface IPedidoRepository
{
    Task AddAsync(
        Guid id,
        Guid idCliente,
        string escopoJson,
        IReadOnlyList<NovaFotoPedidoRequest> fotos,
        CancellationToken cancellationToken = default);
}
