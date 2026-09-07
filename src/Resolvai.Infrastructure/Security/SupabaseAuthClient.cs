using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Security;
using Resolvai.Domain.Exceptions;
using Resolvai.Infrastructure.Options;

namespace Resolvai.Infrastructure.Security;

public sealed class SupabaseAuthClient(HttpClient httpClient, IOptions<SupabaseOptions> options)
    : ISupabaseAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<SupabaseSession> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "auth/v1/token?grant_type=password"
        );
        ApplyAnonHeaders(request, settings);
        request.Content = JsonContent.Create(new { email, password });

        using var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var payload = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        EnsureSuccess(response, payload, "Falha ao autenticar no Supabase.");

        var body = Deserialize<TokenResponse>(payload) ?? throw new UnauthorizedException();

        return body.ToSession();
    }

    public async Task<SupabaseAuthUser> SignUpAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/signup");
        ApplyAnonHeaders(request, settings);
        request.Content = JsonContent.Create(
            new
            {
                email,
                password,
                data = new { name },
            }
        );

        using var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var payload = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (
            response.StatusCode == HttpStatusCode.UnprocessableEntity
            || (
                response.StatusCode == HttpStatusCode.BadRequest
                && payload.Contains("already", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            throw new ConflictException($"Já existe um usuário com o e-mail '{email}'.");
        }

        EnsureSuccess(response, payload, "Falha ao registrar no Supabase Auth.");

        var body =
            Deserialize<SignUpResponse>(payload)
            ?? throw new DomainException("Resposta inválida do Supabase Auth no signup.");

        var user = body.User ?? body.ToUserFallback();
        if (user is null || user.Id == Guid.Empty)
        {
            throw new DomainException("Supabase Auth não devolveu o identificador do usuário.");
        }

        return new SupabaseAuthUser(user.Id, user.Email ?? email);
    }

    public async Task<SupabaseAuthUser> AdminCreateUserAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/admin/users");
        request.Headers.Add("apikey", settings.ServiceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            settings.ServiceRoleKey
        );
        request.Content = JsonContent.Create(
            new
            {
                email,
                password,
                email_confirm = true,
                user_metadata = new { name },
            }
        );

        using var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        var payload = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (
            response.StatusCode == HttpStatusCode.UnprocessableEntity
            || (
                response.StatusCode == HttpStatusCode.BadRequest
                && payload.Contains("already", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            throw new ConflictException($"Já existe um usuário com o e-mail '{email}'.");
        }

        EnsureSuccess(response, payload, "Falha ao criar usuário no Supabase Auth.");

        var body =
            Deserialize<AdminUserResponse>(payload)
            ?? throw new DomainException(
                "Resposta inválida do Supabase Auth na criação administrativa."
            );

        if (body.Id == Guid.Empty)
        {
            throw new DomainException("Supabase Auth não devolveu o identificador do usuário.");
        }

        return new SupabaseAuthUser(body.Id, body.Email ?? email);
    }

    private static void ApplyAnonHeaders(HttpRequestMessage request, SupabaseOptions settings)
    {
        request.Headers.Add("apikey", settings.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AnonKey);
    }

    private static void EnsureSuccess(
        HttpResponseMessage response,
        string payload,
        string errorMessage
    )
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new DomainException(
            $"{errorMessage} Status={(int)response.StatusCode}. Detalhe={Truncate(payload)}"
        );
    }

    private static T? Deserialize<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, JsonOptions);

    private static string Truncate(string value) => value.Length <= 300 ? value : value[..300];

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = "bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("expires_at")]
        public long? ExpiresAt { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("user")]
        public UserDto? User { get; init; }

        public SupabaseSession ToSession()
        {
            if (string.IsNullOrWhiteSpace(AccessToken) || User is null || User.Id == Guid.Empty)
            {
                throw new UnauthorizedException();
            }

            var expiresAt = ExpiresAt is > 0
                ? DateTimeOffset.FromUnixTimeSeconds(ExpiresAt.Value)
                : DateTimeOffset.UtcNow.AddSeconds(ExpiresIn > 0 ? ExpiresIn : 3600);

            return new SupabaseSession(
                AccessToken,
                string.IsNullOrWhiteSpace(TokenType) ? "Bearer" : TokenType,
                expiresAt,
                RefreshToken,
                new SupabaseAuthUser(User.Id, User.Email ?? string.Empty)
            );
        }
    }

    private sealed record SignUpResponse
    {
        [JsonPropertyName("user")]
        public UserDto? User { get; init; }

        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        public UserDto? ToUserFallback() =>
            Id == Guid.Empty ? null : new UserDto { Id = Id, Email = Email };
    }

    private sealed record AdminUserResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }

    private sealed class UserDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }
}
