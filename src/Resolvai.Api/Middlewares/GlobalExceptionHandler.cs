using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Domain.Exceptions;

namespace Resolvai.Api.Middlewares;

/// <summary>
/// Traduz exceções de domínio e de aplicação em respostas ProblemDetails (RFC 9457).
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationException
                .Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray()
                );

            return await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Um ou mais campos são inválidos.",
                        Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                    },
                }
            );
        }

        var (statusCode, title, detail) = Translate(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Erro não tratado em {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path
            );
        }
        else
        {
            logger.LogInformation(
                "Requisição {Method} {Path} rejeitada com {StatusCode}: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                exception.Message
            );
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                },
            }
        );
    }

    private static (int StatusCode, string Title, string Detail) Translate(Exception exception) =>
        exception switch
        {
            UnauthorizedException ex => (
                StatusCodes.Status401Unauthorized,
                "Não autorizado",
                ex.Message
            ),
            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Recurso não encontrado",
                ex.Message
            ),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflito", ex.Message),
            DomainException ex => (
                StatusCodes.Status400BadRequest,
                "Requisição inválida",
                ex.Message
            ),
            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                "Requisição cancelada",
                "A requisição foi cancelada pelo cliente."
            ),
            // Mensagem genérica: detalhes de exceções inesperadas ficam apenas no log.
            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                "Ocorreu um erro inesperado ao processar a requisição."
            ),
        };
}
