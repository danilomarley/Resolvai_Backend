using Resolvai.Domain.Enums;

namespace Resolvai.Application.Contracts.Security;

/// <summary>
/// Expõe o portador do Bearer token da requisição atual sem acoplar a Application ao ASP.NET Core.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? Id { get; }

    string? Email { get; }

    UserRole? Role { get; }
}
