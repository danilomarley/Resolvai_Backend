namespace Resolvai.Application.Contracts.Security;

/// <summary>
/// Nomes de claim curtos, emitidos e lidos sem o mapeamento legado do WS-Federation.
/// </summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}
