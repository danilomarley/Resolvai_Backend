using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.DTOs.Auth;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Application.DTOs.Users;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/auth")]
public sealed class AuthController(
    IAuthService authService,
    IFornecedorService fornecedorService) : ApiControllerBase
{
    /// <summary>
    /// Autentica via Supabase Auth e devolve o Bearer token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
        => Ok(await authService.LoginAsync(request, cancellationToken));

    /// <summary>
    /// Auto-cadastro público no Supabase Auth. O perfil local sempre nasce com o papel Cliente.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);

        return CreatedAtRoute("GetUserById", new { id = user.Id }, user);
    }

    /// <summary>
    /// Cadastra uma conta Prestador e seu perfil profissional PF ou PJ.
    /// </summary>
    [HttpPost("register-provider")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<FornecedorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FornecedorResponse>> RegisterProvider(
        [FromBody] RegisterFornecedorRequest request,
        CancellationToken cancellationToken)
    {
        var fornecedor = await fornecedorService.RegisterAsync(request, cancellationToken);

        return CreatedAtRoute("GetCurrentFornecedor", routeValues: null, value: fornecedor);
    }
}
