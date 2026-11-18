using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexusVerde.Application.Interfaces;

namespace NexusVerde.Api.HealthChecks;

/// <summary>Verifica o serviço externo de clima (Open-Meteo). Indisponível = Degraded: a API segue funcionando.</summary>
public sealed class WeatherHealthCheck(IWeatherClient weather) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var disponivel = await weather.VerificarDisponibilidadeAsync(cancellationToken);
        return disponivel
            ? HealthCheckResult.Healthy("Open-Meteo disponível.")
            : new HealthCheckResult(context.Registration.FailureStatus,
                "Open-Meteo indisponível; as solicitações seguem sem o dado de chuva.");
    }
}

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                tags = e.Value.Tags
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
}
