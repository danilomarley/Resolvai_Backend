using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Resolvai.Application.Contracts.Security;
using Resolvai.Domain.Repositories;
using Resolvai.Infrastructure.Options;

namespace Resolvai.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddSupabaseAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<SupabaseOptions>>((bearer, supabase) =>
            {
                var settings = supabase.Value;
                bearer.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{settings.AuthIssuer}/.well-known/jwks.json",
                    new SupabaseJwksRetriever(settings.AuthIssuer),
                    new HttpDocumentRetriever { RequireHttps = true });
                bearer.RefreshOnIssuerKeyNotFound = true;

                // Sem mapeamento legado: claims chegam com os nomes curtos do JWT do Supabase.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.AuthIssuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    // Compatibilidade com sessões antigas; chaves assimétricas vêm do JWKS.
                    IssuerSigningKey = string.IsNullOrWhiteSpace(settings.JwtSecret) ? null
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AppClaimTypes.Email,
                    RoleClaimType = AppClaimTypes.Role
                };

                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = EnrichWithLocalProfileAsync
                };
            });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// O JWT do Supabase traz role=authenticated. Substituímos pela role do perfil local (Cliente/Prestador/Admin).
    /// Usuário sem perfil local ainda (cadastro não finalizado) segue autenticado só com as claims do Supabase,
    /// para poder chamar a rota de finalização de cadastro.
    /// </summary>
    private static async Task EnrichWithLocalProfileAsync(TokenValidatedContext context)
    {
        var sub = context.Principal?.FindFirstValue(AppClaimTypes.Subject);
        if (!Guid.TryParse(sub, out var userId))
        {
            context.Fail("Token sem identificador de usuário (sub).");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

        if (user is null)
        {
            return;
        }

        if (!user.IsActive)
        {
            context.Fail("Usuário inativo.");
            return;
        }

        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Identidade inválida.");
            return;
        }

        foreach (var claim in identity.FindAll(AppClaimTypes.Role).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(AppClaimTypes.Role, user.Role.ToString()));

        if (identity.FindFirst(AppClaimTypes.Name) is null)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Name, user.Name));
        }

        if (identity.FindFirst(AppClaimTypes.Email) is null)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Email, user.Email.Value));
        }
    }

    private sealed class SupabaseJwksRetriever(string issuer) : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var document = await retriever.GetDocumentAsync(address, cancel);
            var configuration = new OpenIdConnectConfiguration { Issuer = issuer };
            foreach (var key in new JsonWebKeySet(document).GetSigningKeys())
                configuration.SigningKeys.Add(key);
            return configuration;
        }
    }
}
