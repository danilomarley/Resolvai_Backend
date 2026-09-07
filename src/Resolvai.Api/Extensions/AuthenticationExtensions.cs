using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Resolvai.Application.Contracts.Security;
using Resolvai.Domain.Repositories;
using Resolvai.Infrastructure.Options;

namespace Resolvai.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddSupabaseAuthentication(this IServiceCollection services)
    {
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<SupabaseOptions>>(
                (bearer, supabase) =>
                {
                    var settings = supabase.Value;
                    var signingKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.JwtSecret)
                    );

                    // Sem mapeamento legado: claims chegam com os nomes curtos do JWT do Supabase.
                    bearer.MapInboundClaims = false;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.AuthIssuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = AppClaimTypes.Email,
                        RoleClaimType = AppClaimTypes.Role,
                    };

                    bearer.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = EnrichWithLocalProfileAsync,
                    };
                }
            );

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// O JWT do Supabase traz role=authenticated. Substituímos pela role do perfil local (Viewer/Inspector/Admin).
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
            context.Fail("Usuário autenticado no Supabase sem perfil local.");
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
}
