namespace NexusVerde.Application.Interfaces;

/// <summary>Previsão de chuva acumulada para os próximos dias, em mm.</summary>
public sealed record ClimaPrevisto(double PrecipitacaoTotalMm);

/// <summary>Serviço externo de clima (Open-Meteo) usado no cálculo de risco/prioridade.</summary>
public interface IWeatherClient
{
    /// <summary>Retorna null quando o serviço está indisponível (a API segue funcionando sem o dado).</summary>
    Task<ClimaPrevisto?> ObterPrevisaoAsync(double latitude, double longitude, CancellationToken ct = default);

    Task<bool> VerificarDisponibilidadeAsync(CancellationToken ct = default);
}
