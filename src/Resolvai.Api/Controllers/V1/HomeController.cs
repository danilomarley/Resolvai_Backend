using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/home")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HomeController(IPedidoService pedidoService) : ApiControllerBase
{
    [HttpGet("summary")]
    [EndpointSummary("Resumo dos pedidos do usuário autenticado para a Home.")]
    [EndpointDescription("Dados dos cards (totais por status, ativos, novos em 7 dias e propostas) e até 5 pedidos mais recentes por data de criação. Sem pedidos, retorna contadores zerados e lista vazia.")]
    [ProducesResponseType<ResumoHomeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumoHomeResponse>> GetSummary(CancellationToken cancellationToken)
        => Ok(await pedidoService.GetSummaryAsync(cancellationToken));
}
