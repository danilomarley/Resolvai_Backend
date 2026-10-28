using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Domain.Exceptions;
using Resolvai.Infrastructure.Options;
using Resolvai.Infrastructure.Security;

internal static class AuthClientChecks
{
    private const string TestEmail = "auth@example.invalid";
    private const string TestPassword = "SenhaSomenteParaTeste!123";

    public static async Task RunAsync()
    {
        await CheckFreshSignUpAsync(nested: false);
        await CheckFreshSignUpAsync(nested: true);
        await CheckObfuscatedResponseAsync();
        await CheckExistingUnconfirmedResponseAsync();
        await CheckExistingAccountErrorAsync();
        await CheckRecoveryEmailAsync();
        await CheckInvalidCreationResponseAsync();
        await CheckErrorPrivacyAsync();
    }

    private static async Task CheckFreshSignUpAsync(bool nested)
    {
        var id = Guid.NewGuid();
        var calls = 0;
        using var fixture = new ClientFixture(async (request, cancellationToken) =>
        {
            calls++;
            Check(request.RequestUri?.AbsolutePath == "/auth/v1/signup", "Cadastro novo usa endpoint de signup");
            var nonce = await ReadNonceAsync(request, cancellationToken);
            Check(!string.IsNullOrWhiteSpace(nonce), "Signup envia correlação de criação gerada pelo servidor");
            var user = UserPayload(id, TestEmail, nonce, withIdentity: true);
            return Json(HttpStatusCode.OK, nested ? new { user } : user);
        });

        var result = await fixture.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste");
        Check(result.Id == id && result.Email == TestEmail && calls == 1,
            "Aceita resposta de criação correlacionada sem autenticar novamente");
    }

    private static async Task CheckObfuscatedResponseAsync()
    {
        var obfuscatedId = Guid.NewGuid();
        var recoveredId = Guid.NewGuid();
        var tokenCalls = 0;
        using var fixture = new ClientFixture(async (request, cancellationToken) =>
        {
            if (request.RequestUri?.AbsolutePath == "/auth/v1/signup")
            {
                var nonce = await ReadNonceAsync(request, cancellationToken);
                // O Auth pode ecoar os metadados com UUID fictício e identities vazio.
                return Json(HttpStatusCode.OK, UserPayload(obfuscatedId, TestEmail, nonce, withIdentity: false));
            }

            tokenCalls++;
            return Session(recoveredId, TestEmail);
        });

        var result = await fixture.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste");
        Check(tokenCalls == 1 && result.Id == recoveredId && result.Id != obfuscatedId,
            "Resposta ofuscada só é recuperada pela identidade autenticada com senha");

        using var denied = new ClientFixture(async (request, cancellationToken) =>
        {
            if (request.RequestUri?.AbsolutePath == "/auth/v1/signup")
            {
                return Json(HttpStatusCode.OK,
                    UserPayload(obfuscatedId, TestEmail, await ReadNonceAsync(request, cancellationToken), withIdentity: false));
            }

            return Json(HttpStatusCode.BadRequest, new { error_code = "invalid_credentials", msg = "Invalid credentials" });
        });
        await ThrowsAsync<ConflictException>(() => denied.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));
    }

    private static async Task CheckExistingUnconfirmedResponseAsync()
    {
        var id = Guid.NewGuid();
        var tokenCalls = 0;
        using var fixture = new ClientFixture((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/auth/v1/signup")
            {
                return Task.FromResult(Json(HttpStatusCode.OK,
                    UserPayload(id, TestEmail, "tentativa-antiga", withIdentity: true)));
            }

            tokenCalls++;
            return Task.FromResult(Json(HttpStatusCode.BadRequest,
                new { error_code = "email_not_confirmed", msg = "Email not confirmed" }));
        });

        await ThrowsAsync<ConflictException>(() => fixture.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));
        Check(tokenCalls == 1, "Conta não confirmada não é adotada sem prova de posse");
    }

    private static async Task CheckExistingAccountErrorAsync()
    {
        var id = Guid.NewGuid();
        var tokenCalls = 0;
        using var fixture = new ClientFixture((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/auth/v1/signup")
            {
                return Task.FromResult(Json(HttpStatusCode.UnprocessableEntity,
                    new { code = 422, error_code = "user_already_exists", msg = "User already registered" }));
            }

            tokenCalls++;
            return Task.FromResult(Session(id, TestEmail));
        });

        var result = await fixture.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste");
        Check(result.Id == id && tokenCalls == 1, "Conta Auth existente exige autenticação antes de recuperar cadastro");

        tokenCalls = 0;
        using var weakPassword = new ClientFixture((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/auth/v1/token")
            {
                tokenCalls++;
            }

            return Task.FromResult(Json(HttpStatusCode.UnprocessableEntity,
                new { error_code = "weak_password", msg = "Password too weak" }));
        });
        await ThrowsAsync<DomainException>(() => weakPassword.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));
        Check(tokenCalls == 0, "Erro de senha não é confundido com duplicidade recuperável");
    }

    private static async Task CheckRecoveryEmailAsync()
    {
        using var fixture = new ClientFixture((request, _) => Task.FromResult(
            request.RequestUri?.AbsolutePath == "/auth/v1/signup"
                ? Json(HttpStatusCode.UnprocessableEntity, new { code = "user_already_exists" })
                : Session(Guid.NewGuid(), "outro@example.invalid")));

        await ThrowsAsync<ConflictException>(() => fixture.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));
    }

    private static async Task CheckInvalidCreationResponseAsync()
    {
        using var wrongEmail = new ClientFixture(async (request, cancellationToken) =>
            Json(HttpStatusCode.OK, UserPayload(Guid.NewGuid(), "outro@example.invalid",
                await ReadNonceAsync(request, cancellationToken), withIdentity: true)));
        await ThrowsAsync<DomainException>(() => wrongEmail.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));

        using var emptyId = new ClientFixture(async (request, cancellationToken) =>
            Json(HttpStatusCode.OK, UserPayload(Guid.Empty, TestEmail,
                await ReadNonceAsync(request, cancellationToken), withIdentity: true)));
        await ThrowsAsync<DomainException>(() => emptyId.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));

        using var malformed = new ClientFixture((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{", Encoding.UTF8, "application/json")
        }));
        await ThrowsAsync<DomainException>(() => malformed.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste"));
    }

    private static async Task CheckErrorPrivacyAsync()
    {
        const string privateMarker = "payload-externo-que-nao-pode-vazar";
        using var fixture = new ClientFixture((_, _) => Task.FromResult(
            Json(HttpStatusCode.InternalServerError, new { msg = privateMarker })));

        try
        {
            await fixture.Client.SignUpAsync(TestEmail, TestPassword, "Perfil de teste");
        }
        catch (Exception exception)
        {
            Check(!exception.Message.Contains(privateMarker, StringComparison.Ordinal)
                && !exception.Message.Contains(TestEmail, StringComparison.Ordinal),
                "Falhas do provedor não expõem payload nem e-mail em mensagem de exceção");
            return;
        }

        throw new Exception("Era esperada falha do provedor simulado.");
    }

    private static async Task<string?> ReadNonceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("data").GetProperty("registration_attempt").GetString();
    }

    private static object UserPayload(Guid id, string email, string? nonce, bool withIdentity)
    {
        object[] identities = withIdentity ? [new { provider = "email", user_id = id }] : [];
        return new
        {
            id,
            email,
            user_metadata = new { registration_attempt = nonce },
            identities
        };
    }

    private static HttpResponseMessage Session(Guid id, string email)
        => Json(HttpStatusCode.OK, new
        {
            access_token = "token-sintetico",
            token_type = "bearer",
            expires_in = 60,
            user = new { id, email }
        });

    private static HttpResponseMessage Json(HttpStatusCode status, object payload)
        => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new Exception($"Era esperada a exceção {typeof(T).Name}.");
    }

    private sealed class ClientFixture : IDisposable
    {
        private readonly HttpClient _http;
        public SupabaseAuthClient Client { get; }

        public ClientFixture(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _http = new HttpClient(new FakeHandler(send)) { BaseAddress = new Uri("https://auth.example.invalid/") };
            Client = new SupabaseAuthClient(_http, Options.Create(new SupabaseOptions
            {
                Url = "https://auth.example.invalid",
                AnonKey = "chave-sintetica-sem-acesso"
            }));
        }

        public void Dispose() => _http.Dispose();
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}
