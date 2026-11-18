using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Exceptions;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Api.Middleware;

/// <summary>Traduz exceções em respostas ProblemDetails (RFC 7807) com o correlationId.</summary>
public sealed class GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito com o estado atual"),
            ExternalServiceException => (StatusCodes.Status503ServiceUnavailable, "Serviço dependente indisponível"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflito ao gravar os dados"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno inesperado")
        };

        if (status >= 500)
            logger.LogError(exception, "Erro não tratado em {Path}", httpContext.Request.Path);
        else
            logger.LogWarning("{Tipo} em {Path}: {Mensagem}", exception.GetType().Name, httpContext.Request.Path, exception.Message);

        var detail = status switch
        {
            >= 500 when !environment.IsDevelopment() => "Ocorreu um erro interno. Informe o correlationId ao suporte.",
            StatusCodes.Status409Conflict when exception is DbUpdateException =>
                "A operação viola uma restrição de integridade (registro duplicado ou em uso).",
            _ => exception.Message
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;
        if (Activity.Current is { } atividade)
            problem.Extensions["traceId"] = atividade.TraceId.ToString();

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
        return true;
    }
}
