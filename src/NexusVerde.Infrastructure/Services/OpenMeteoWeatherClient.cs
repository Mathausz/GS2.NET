using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusVerde.Application.Interfaces;

namespace NexusVerde.Infrastructure.Services;

/// <summary>
/// Cliente da API pública Open-Meteo (sem chave). Usado para estimar a chuva dos próximos dias,
/// que entra no cálculo de prioridade. Falhas nunca derrubam a API: o dado simplesmente fica ausente.
/// </summary>
public sealed class OpenMeteoWeatherClient(HttpClient http, ILogger<OpenMeteoWeatherClient> logger) : IWeatherClient
{
    public async Task<ClimaPrevisto?> ObterPrevisaoAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"v1/forecast?latitude={latitude:F4}&longitude={longitude:F4}&daily=precipitation_sum&forecast_days=3&timezone=auto");

            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Open-Meteo respondeu {Status}; seguindo sem dado de chuva.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("daily", out var daily) ||
                !daily.TryGetProperty("precipitation_sum", out var soma) ||
                soma.ValueKind != JsonValueKind.Array)
                return null;

            double total = 0;
            foreach (var item in soma.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                    total += item.GetDouble();
            }

            return new ClimaPrevisto(Math.Round(total, 1));
        }
        catch (Exception ex) when (!ct.IsCancellationRequested &&
                                   ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Falha ao consultar Open-Meteo; seguindo sem dado de chuva.");
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadeAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync("v1/forecast?latitude=0&longitude=0&current=temperature_2m", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested &&
                                   ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}
