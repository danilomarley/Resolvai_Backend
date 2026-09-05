namespace Resolvai.Application.Contracts.Security;

public interface ISupabaseAuthClient
{
    Task<SupabaseSession> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<SupabaseAuthUser> SignUpAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default);

    Task<SupabaseAuthUser> AdminCreateUserAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default);
}

public sealed record SupabaseSession(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    string? RefreshToken,
    SupabaseAuthUser User);

public sealed record SupabaseAuthUser(Guid Id, string Email);
