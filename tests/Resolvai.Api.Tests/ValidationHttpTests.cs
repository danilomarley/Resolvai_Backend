using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resolvai.Api.Controllers.V1;
using Resolvai.Api.Middlewares;
using Resolvai.Api.Security;
using Resolvai.Application;
using Resolvai.Application.Contracts.Security;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Api.Tests;

// Exercita controllers e services reais com banco e Supabase substituídos em memória.
public class ValidationHttpTests : IDisposable
{
    private readonly WebApplication app;
    private readonly HttpClient client;
    private readonly FakeDependencies dependencies = new();

    public ValidationHttpTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var services = builder.Services;
        services
            .AddControllers(options =>
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true
            )
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                )
            );
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddApplication();
        services.AddSingleton<IUserRepository>(dependencies);
        services.AddSingleton<ISupabaseAuthClient>(dependencies);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        services.AddAuthorization();
        app = builder.Build();
        app.UseExceptionHandler();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.StartAsync().GetAwaiter().GetResult();
        client = app.GetTestClient();
    }

    [Theory]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/users")]
    public async Task Invalid_input_returns_field_errors_without_external_calls(string route)
    {
        client.DefaultRequestHeaders.Add("X-Test-Admin", "true");
        var response = await client.PostAsJsonAsync(
            route,
            new
            {
                name = " ",
                email = "invalid",
                password = "",
                role = 999,
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.GetProperty("errors").TryGetProperty("Email", out _));
        Assert.True(body.GetProperty("errors").TryGetProperty("Password", out _));
        Assert.Equal(0, dependencies.Calls);
    }

    [Fact]
    public async Task Explicit_null_fields_are_handled_by_fluent_validation()
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                name = (string?)null,
                email = (string?)null,
                password = (string?)null,
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("errors").EnumerateObject().Count());
        Assert.Equal(0, dependencies.Calls);
    }

    [Fact]
    public async Task Registration_and_login_preserve_successful_flow()
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                name = " Maria ",
                email = "MARIA@example.com",
                password = "Senha123",
            }
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Maria", dependencies.User!.Name);
        Assert.Equal("maria@example.com", dependencies.User.Email.Value);
        Assert.Equal(UserRole.Viewer, dependencies.User.Role);
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "maria@example.com", password = "Senha123" }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("test-token", body.GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task Administrative_creation_and_false_status_are_accepted()
    {
        client.DefaultRequestHeaders.Add("X-Test-Admin", "true");
        var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                name = "Maria",
                email = "maria@example.com",
                password = "Senha123",
                role = "Inspector",
            }
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(UserRole.Inspector, dependencies.User!.Role);
        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/users/{dependencies.User.Id}/status",
            new { isActive = false }
        );
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.False(dependencies.User.IsActive);
        var missing = await client.PatchAsJsonAsync(
            $"/api/v1/users/{dependencies.User.Id}/status",
            new { }
        );
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task Anonymous_user_cannot_create_administrative_profile()
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                name = "Maria",
                email = "maria@example.com",
                password = "Senha123",
            }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, dependencies.Calls);
    }

    public void Dispose()
    {
        client.Dispose();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class FakeDependencies : IUserRepository, ISupabaseAuthClient
    {
        public int Calls { get; private set; }
        public User? User { get; private set; }
        private readonly Guid id = Guid.NewGuid();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(User);
        }

        public Task<User?> GetByEmailAsync(
            Email email,
            CancellationToken cancellationToken = default
        )
        {
            Calls++;
            return Task.FromResult(User);
        }

        public Task<bool> ExistsByEmailAsync(
            Email email,
            CancellationToken cancellationToken = default
        )
        {
            Calls++;
            return Task.FromResult(User is not null);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<User>>(User is null ? [] : [User]);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Calls++;
            User = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            Calls++;
            User = user;
            return Task.CompletedTask;
        }

        public Task<SupabaseAuthUser> SignUpAsync(
            string email,
            string password,
            string name,
            CancellationToken cancellationToken = default
        )
        {
            Calls++;
            return Task.FromResult(new SupabaseAuthUser(id, email));
        }

        public Task<SupabaseAuthUser> AdminCreateUserAsync(
            string email,
            string password,
            string name,
            CancellationToken cancellationToken = default
        ) => SignUpAsync(email, password, name, cancellationToken);

        public Task<SupabaseSession> SignInWithPasswordAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default
        )
        {
            Calls++;
            return Task.FromResult(
                new SupabaseSession(
                    "test-token",
                    "Bearer",
                    DateTimeOffset.UtcNow.AddHours(1),
                    null,
                    new SupabaseAuthUser(id, email)
                )
            );
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Admin"))
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], Scheme.Name);
            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)
                )
            );
        }
    }
}
