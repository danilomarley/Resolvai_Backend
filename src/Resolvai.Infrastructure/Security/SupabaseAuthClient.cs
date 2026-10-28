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

public sealed class SupabaseAuthClient(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options) : ISupabaseAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<SupabaseSession> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/token?grant_type=password");
        ApplyAnonHeaders(request, settings);
        request.Content = JsonContent.Create(new { email, password });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException();
        }

        EnsureSuccess(response, "Falha ao autenticar no Supabase.");

        var body = Deserialize<TokenResponse>(payload)
            ?? throw new UnauthorizedException();

        return body.ToSession();
    }

    public async Task<SupabaseAuthUser> SignUpAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/signup");
        var registrationAttempt = Guid.NewGuid().ToString("N");
        ApplyAnonHeaders(request, settings);
        request.Content = JsonContent.Create(new
        {
            email,
            password,
            data = new { name, registration_attempt = registrationAttempt }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (IsExistingUser(response, payload))
        {
            return await RecoverUserAsync(email, password, cancellationToken);
        }

        if ((int)response.StatusCode >= 500)
        {
            throw new RegistrationPendingException();
        }

        EnsureSuccess(response, "Falha ao registrar no Supabase Auth.");

        var body = Deserialize<SignUpResponse>(payload)
            ?? throw new DomainException("Resposta inválida do Supabase Auth no signup.");

        var user = body.User ?? body.ToUserFallback();
        if (user is null || user.Id == Guid.Empty
            || !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Não foi possível confirmar a identidade da conta criada.");
        }

        // O signup pode devolver um UUID ofuscado ou uma conta ainda não confirmada
        // já existente. Nunca adotar esses perfis sem comprovar a senha.
        // O nonce identifica somente esta criação; não concede papel ou autorização.
        var isNewUser = user.Identities is { Length: > 0 }
            && user.Metadata.ValueKind == JsonValueKind.Object
            && user.Metadata.TryGetProperty("registration_attempt", out var attempt)
            && attempt.ValueKind == JsonValueKind.String
            && attempt.GetString() == registrationAttempt;

        return isNewUser
            ? new SupabaseAuthUser(user.Id, user.Email!)
            : await RecoverUserAsync(email, password, cancellationToken);
    }

    public async Task<SupabaseAuthUser> AdminCreateUserAsync(
        string email,
        string password,
        string name,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "auth/v1/admin/users");
        request.Headers.Add("apikey", settings.ServiceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ServiceRoleKey);
        request.Content = JsonContent.Create(new
        {
            email,
            password,
            email_confirm = true,
            user_metadata = new { name }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (IsExistingUser(response, payload))
        {
            throw new ConflictException("E-mail já cadastrado.");
        }

        EnsureSuccess(response, "Falha ao criar usuário no Supabase Auth.");

        var body = Deserialize<AdminUserResponse>(payload)
            ?? throw new DomainException("Resposta inválida do Supabase Auth na criação administrativa.");

        if (body.Id == Guid.Empty)
        {
            throw new DomainException("Supabase Auth não devolveu o identificador do usuário.");
        }

        return new SupabaseAuthUser(body.Id, body.Email ?? email);
    }

    private async Task<SupabaseAuthUser> RecoverUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await SignInWithPasswordAsync(email, password, cancellationToken);
            if (session.User.Id == Guid.Empty
                || !string.Equals(session.User.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedException();
            }

            return session.User;
        }
        catch (UnauthorizedException)
        {
            throw new ConflictException("Não foi possível retomar o cadastro. Confirme seu e-mail e tente novamente com as mesmas credenciais.");
        }
    }

    private static bool IsExistingUser(HttpResponseMessage response, string payload)
        => (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            && (GetErrorCode(payload) is "user_already_exists" or "email_exists");

    private static string? GetErrorCode(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (document.RootElement.TryGetProperty("error_code", out var errorCode) && errorCode.ValueKind == JsonValueKind.String)
            {
                return errorCode.GetString();
            }

            return document.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String
                ? code.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ApplyAnonHeaders(HttpRequestMessage request, SupabaseOptions settings)
    {
        request.Headers.Add("apikey", settings.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AnonKey);
    }

    private static void EnsureSuccess(HttpResponseMessage response, string errorMessage)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // O payload do provedor pode conter dados pessoais ou detalhes internos.
        throw new DomainException($"{errorMessage} Status={(int)response.StatusCode}.");
    }

    private static T? Deserialize<T>(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            throw new DomainException("Resposta inválida do serviço de autenticação.");
        }
    }

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
                new SupabaseAuthUser(User.Id, User.Email ?? string.Empty));
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

        [JsonPropertyName("identities")]
        public JsonElement[]? Identities { get; init; }

        [JsonPropertyName("user_metadata")]
        public JsonElement Metadata { get; init; }

        public UserDto? ToUserFallback() =>
            Id == Guid.Empty ? null : new UserDto { Id = Id, Email = Email, Identities = Identities, Metadata = Metadata };
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

        [JsonPropertyName("identities")]
        public JsonElement[]? Identities { get; init; }

        [JsonPropertyName("user_metadata")]
        public JsonElement Metadata { get; init; }
    }
}
