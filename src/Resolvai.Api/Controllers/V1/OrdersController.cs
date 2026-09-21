using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.DTOs.Orders;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta os detalhes de um pedido do usuário autenticado.")]
    [EndpointDescription("Retorna 404 quando o pedido não existe ou pertence a outro usuário.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await orderService.GetByIdAsync(id, cancellationToken));
}
