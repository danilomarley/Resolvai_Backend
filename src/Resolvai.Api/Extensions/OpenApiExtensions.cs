using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Resolvai.Api.Extensions;

public static class OpenApiExtensions
{
    private const string SchemeId = "Bearer";

    public static IServiceCollection AddOpenApiWithBearer(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
        });

    /// <summary>
    /// Publica o esquema de segurança HTTP Bearer no documento.
    /// </summary>
    private sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken
        )
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Access token JWT do Supabase Auth (POST /api/v1/auth/login ou client SDK).",
            };

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Exige o Bearer apenas nas operações que de fato estão protegidas.
    /// </summary>
    private sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken
        )
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;

            if (
                metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any()
            )
            {
                return Task.CompletedTask;
            }

            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(SchemeId, context.Document)] = [],
                },
            ];

            return Task.CompletedTask;
        }
    }
}
