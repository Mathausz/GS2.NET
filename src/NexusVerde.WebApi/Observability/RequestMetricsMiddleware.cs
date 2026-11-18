using System.Diagnostics;

namespace NexusVerde.Api.Observability;

/// <summary>Mede o tempo de resposta e o status de cada requisição (exceto /metrics) e alimenta o <see cref="MetricsRegistry"/>.</summary>
public sealed class RequestMetricsMiddleware(RequestDelegate next, MetricsRegistry registry)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/metrics"))
        {
            await next(context);
            return;
        }

        var inicio = Stopwatch.GetTimestamp();
        var statusForcado = 0;

        try
        {
            await next(context);
        }
        catch
        {
            statusForcado = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            var duracaoMs = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
            var rota = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "nao-mapeada";
            var status = statusForcado != 0 ? statusForcado : context.Response.StatusCode;
            registry.RegistrarRequisicao(context.Request.Method, rota, status, duracaoMs);
        }
    }
}
