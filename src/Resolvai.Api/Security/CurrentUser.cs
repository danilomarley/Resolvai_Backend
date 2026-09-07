using System.Security.Claims;
using Resolvai.Application.Contracts.Security;
using Resolvai.Domain.Enums;

namespace Resolvai.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? Id => Guid.TryParse(FindClaim(AppClaimTypes.Subject), out var id) ? id : null;

    public string? Email => FindClaim(AppClaimTypes.Email);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(FindClaim(AppClaimTypes.Role), out var role) ? role : null;

    private string? FindClaim(string type) => Principal?.FindFirstValue(type);
}
