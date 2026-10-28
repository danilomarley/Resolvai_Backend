using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/especialidades")]
public sealed class EspecialidadesController(IEspecialidadeService especialidadeService) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<EspecialidadeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EspecialidadeResponse>>> List(CancellationToken cancellationToken)
        => Ok(await especialidadeService.ListActiveAsync(cancellationToken));
}
