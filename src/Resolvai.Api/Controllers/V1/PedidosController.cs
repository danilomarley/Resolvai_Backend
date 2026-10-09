using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PedidosController(IPedidoService pedidoService) : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Lista paginada dos pedidos do usuário autenticado (Seus Pedidos).")]
    [EndpointDescription("Ordenada por data de criação decrescente. Filtro opcional por status. page a partir de 1 e pageSize entre 1 e 50 (padrão 10).")]
    [ProducesResponseType<PaginaResponse<PedidoResumoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaResponse<PedidoResumoResponse>>> List(
        [FromQuery] ListarPedidosQuery query,
        CancellationToken cancellationToken)
        => Ok(await pedidoService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta os detalhes de um pedido do usuário autenticado.")]
    [EndpointDescription("Inclui escopo, fotos e propostas recebidas. Retorna 404 quando o pedido não existe ou pertence a outro usuário.")]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await pedidoService.GetByIdAsync(id, cancellationToken));
}
