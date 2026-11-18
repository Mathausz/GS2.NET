using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NexusVerde.Api.Endpoints;
using NexusVerde.Api.HealthChecks;
using NexusVerde.Api.Middleware;
using NexusVerde.Api.Observability;
using NexusVerde.Api.Security;
using Serilog;

namespace NexusVerde.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Ordem do pipeline: proxy → correlationId → métricas → logs → tratamento de erros → Swagger → CORS →
    /// autenticação → rate limiting → autorização → idempotência → endpoints.
    /// O log de requisições fica antes do tratamento de erros para registrar o status real devolvido
    /// (ex.: 400/404), e não 500, quando uma exceção é convertida em ProblemDetails.
    /// </summary>
    public static WebApplication UseNexusVerdePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestMetricsMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();

        if (app.Configuration.GetValue("Swagger:Enabled", true))
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nexus Verde API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Nexus Verde API";
            });
        }

        if (app.Environment.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseCors(CorsPolicies.App);

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseMiddleware<IdempotencyMiddleware>();

        return app;
    }

    public static WebApplication MapNexusVerdeEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthResponseWriter.WriteAsync
        }).AllowAnonymous();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        }).AllowAnonymous();

        app.MapNexusVerdeMetrics();

        app.MapAuthEndpoints();
        app.MapUsuarioEndpoints();
        app.MapRegiaoEndpoints();
        app.MapDesastreEndpoints();
        app.MapPontoAguaEndpoints();
        app.MapSolicitacaoEndpoints();
        app.MapAtendimentoEndpoints();
        app.MapOrientacaoEndpoints();

        return app;
    }
}
