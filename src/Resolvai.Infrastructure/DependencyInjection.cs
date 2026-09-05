using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resolvai.Application.Contracts.Security;
using Resolvai.Domain.Repositories;
using Resolvai.Infrastructure.Options;
using Resolvai.Infrastructure.Persistence.Connection;
using Resolvai.Infrastructure.Persistence.Repositories;
using Resolvai.Infrastructure.Persistence.TypeHandlers;
using Resolvai.Infrastructure.Security;

namespace Resolvai.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    options.ConnectionString =
                        configuration.GetConnectionString("Supabase")
                        ?? configuration.GetConnectionString("Postgres")
                        ?? string.Empty;
                }
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database:ConnectionString (ou ConnectionStrings:Supabase) não foi configurada.")
            .ValidateOnStart();

        services.AddOptions<SupabaseOptions>()
            .Bind(configuration.GetSection(SupabaseOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Url), "Supabase:Url não foi configurada.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AnonKey), "Supabase:AnonKey não foi configurada.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ServiceRoleKey), "Supabase:ServiceRoleKey não foi configurada.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.JwtSecret), "Supabase:JwtSecret não foi configurada.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Supabase:Audience não foi configurada.")
            .ValidateOnStart();

        DapperTypeHandlers.Register();

        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddHttpClient<ISupabaseAuthClient, SupabaseAuthClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SupabaseOptions>>().Value;
            client.BaseAddress = new Uri(settings.Url.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
