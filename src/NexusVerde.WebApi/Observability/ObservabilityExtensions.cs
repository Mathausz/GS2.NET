using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using NexusVerde.Application.Telemetry;

namespace NexusVerde.Api.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Tracing distribuído e métricas com OpenTelemetry: instrumentação do ASP.NET Core e do EF Core, mais as
    /// fontes próprias do Nexus Verde (spans nos services; métricas de negócio).
    /// O exporter de console (útil para demonstrar) é ligado por <c>Observability:ConsoleExporter</c>.
    /// </summary>
    public static IServiceCollection AddNexusVerdeObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MetricsRegistry>();

        var exportarNoConsole = configuration.GetValue("Observability:ConsoleExporter", false);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("NexusVerde.Api"))
            .WithTracing(tracing =>
            {
                tracing.AddSource(NexusVerdeTelemetry.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
                if (exportarNoConsole) tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(NexusVerdeTelemetry.Name)
                    .AddAspNetCoreInstrumentation();
                if (exportarNoConsole) metrics.AddConsoleExporter();
            });

        return services;
    }

    /// <summary>GET /metrics: métricas de desempenho e de negócio em texto (formato Prometheus).</summary>
    public static WebApplication MapNexusVerdeMetrics(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Observability:MetricsEnabled", true))
            return app;

        app.MapGet("/metrics", (MetricsRegistry registry) =>
                Results.Text(registry.RenderizarPrometheus(), "text/plain; version=0.0.4; charset=utf-8"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }
}
