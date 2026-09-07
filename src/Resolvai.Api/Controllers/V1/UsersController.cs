using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.Users;
using Resolvai.Application.Users.DTOs;
using Resolvai.Domain.Enums;

namespace Resolvai.Api.Controllers.V1;

[Route("api/v1/users")]
public sealed class UsersController(IUserService userService) : ApiControllerBase
{
    /// <summary>
    /// Dados do usuário dono do token informado.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> GetCurrent(CancellationToken cancellationToken) =>
        Ok(await userService.GetCurrentAsync(cancellationToken));

    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(
        CancellationToken cancellationToken
    ) => Ok(await userService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}", Name = "GetUserById")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken
    ) => Ok(await userService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Criação administrativa, única forma de nascer com papel diferente de Viewer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken
    )
    {
        var user = await userService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute("GetUserById", new { id = user.Id }, user);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> SetStatus(
        Guid id,
        [FromBody] SetUserStatusRequest request,
        CancellationToken cancellationToken
    ) => Ok(await userService.SetActiveAsync(id, request.IsActive, cancellationToken));
}
