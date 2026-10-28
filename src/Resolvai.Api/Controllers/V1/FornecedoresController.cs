using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Application.Services.Interfaces;
using Resolvai.Domain.Enums;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/fornecedores")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FornecedoresController(IFornecedorService fornecedorService) : ApiControllerBase
{
    [HttpGet("me", Name = "GetCurrentFornecedor")]
    [Authorize(Roles = nameof(UserRole.Prestador))]
    [ProducesResponseType<FornecedorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FornecedorResponse>> GetCurrent(CancellationToken cancellationToken)
        => Ok(await fornecedorService.GetCurrentAsync(cancellationToken));
}
