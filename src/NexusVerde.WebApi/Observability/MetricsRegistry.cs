using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using NexusVerde.Application.Telemetry;

namespace NexusVerde.Api.Observability;

/// <summary>
/// Agrega as métricas de desempenho (tempo de resposta e taxa de erros por rota) e as métricas de negócio
/// do <see cref="NexusVerdeTelemetry"/> e as expõe em texto no formato Prometheus (GET /metrics).
/// Tudo em memória do processo: simples, sem dependências externas e fácil de demonstrar.
/// </summary>
public sealed class MetricsRegistry : IDisposable
{
    private static readonly double[] LimitesMs = { 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 };

    private sealed class Histograma
    {
        public readonly object Trava = new();
        public readonly long[] Acumulado = new long[LimitesMs.Length];
        public long Total;
        public double SomaMs;
    }

    private readonly ConcurrentDictionary<(string Metodo, string Rota, int Status), long> _requisicoes = new();
    private readonly ConcurrentDictionary<(string Metodo, string Rota), Histograma> _duracoes = new();
    private readonly ConcurrentDictionary<string, long> _contadoresNegocio = new();
    private readonly ConcurrentDictionary<string, string> _ajudaNegocio = new();
    private readonly MeterListener _listener;
    private readonly DateTime _inicio = DateTime.UtcNow;

    public MetricsRegistry()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrumento, listener) =>
            {
                if (instrumento.Meter.Name == NexusVerdeTelemetry.Name)
                    listener.EnableMeasurementEvents(instrumento);
            }
        };
        _listener.SetMeasurementEventCallback<long>(AoMedirLong);
        _listener.Start();
    }

    public void RegistrarRequisicao(string metodo, string rota, int status, double duracaoMs)
    {
        _requisicoes.AddOrUpdate((metodo, rota, status), 1, (_, atual) => atual + 1);

        var histograma = _duracoes.GetOrAdd((metodo, rota), _ => new Histograma());
        lock (histograma.Trava)
        {
            for (var i = 0; i < LimitesMs.Length; i++)
                if (duracaoMs <= LimitesMs[i]) histograma.Acumulado[i]++;
            histograma.Total++;
            histograma.SomaMs += duracaoMs;
        }
    }

    private void AoMedirLong(Instrument instrumento, long valor, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? estado)
    {
        var nome = instrumento.Name.Replace('.', '_') + "_total";
        var rotulos = new List<string>();
        foreach (var tag in tags)
            rotulos.Add($"{tag.Key}=\"{Escapar(tag.Value?.ToString())}\"");
        rotulos.Sort(StringComparer.Ordinal);

        var juntos = string.Join(",", rotulos);
        var chave = rotulos.Count == 0 ? nome : $"{nome}{{{juntos}}}";
        _contadoresNegocio.AddOrUpdate(chave, valor, (_, atual) => atual + valor);
        _ajudaNegocio[nome] = instrumento.Description ?? nome;
    }

    private static string Escapar(string? valor) =>
        (valor ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");

    private static string D(double valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);

    public string RenderizarPrometheus()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# HELP nexusverde_http_requests_total Total de requisições HTTP por método, rota e status.");
        sb.AppendLine("# TYPE nexusverde_http_requests_total counter");
        foreach (var item in _requisicoes.OrderBy(x => x.Key.Rota).ThenBy(x => x.Key.Metodo).ThenBy(x => x.Key.Status))
            sb.AppendLine($"nexusverde_http_requests_total{{method=\"{item.Key.Metodo}\",route=\"{Escapar(item.Key.Rota)}\",status=\"{item.Key.Status}\"}} {item.Value}");

        sb.AppendLine("# HELP nexusverde_http_request_duration_ms Tempo de resposta das requisições, em milissegundos.");
        sb.AppendLine("# TYPE nexusverde_http_request_duration_ms histogram");
        foreach (var item in _duracoes.OrderBy(x => x.Key.Rota).ThenBy(x => x.Key.Metodo))
        {
            var rotulo = $"method=\"{item.Key.Metodo}\",route=\"{Escapar(item.Key.Rota)}\"";
            lock (item.Value.Trava)
            {
                for (var i = 0; i < LimitesMs.Length; i++)
                    sb.AppendLine($"nexusverde_http_request_duration_ms_bucket{{{rotulo},le=\"{D(LimitesMs[i])}\"}} {item.Value.Acumulado[i]}");
                sb.AppendLine($"nexusverde_http_request_duration_ms_bucket{{{rotulo},le=\"+Inf\"}} {item.Value.Total}");
                sb.AppendLine($"nexusverde_http_request_duration_ms_sum{{{rotulo}}} {D(item.Value.SomaMs)}");
                sb.AppendLine($"nexusverde_http_request_duration_ms_count{{{rotulo}}} {item.Value.Total}");
            }
        }

        var total = _requisicoes.Values.Sum();
        var erros4xx = _requisicoes.Where(x => x.Key.Status is >= 400 and < 500).Sum(x => x.Value);
        var erros5xx = _requisicoes.Where(x => x.Key.Status >= 500).Sum(x => x.Value);

        sb.AppendLine("# HELP nexusverde_http_errors_total Respostas de erro por classe (4xx cliente, 5xx servidor).");
        sb.AppendLine("# TYPE nexusverde_http_errors_total counter");
        sb.AppendLine($"nexusverde_http_errors_total{{class=\"4xx\"}} {erros4xx}");
        sb.AppendLine($"nexusverde_http_errors_total{{class=\"5xx\"}} {erros5xx}");

        sb.AppendLine("# HELP nexusverde_http_error_ratio Taxa de erros: respostas 4xx/5xx sobre o total (0 a 1).");
        sb.AppendLine("# TYPE nexusverde_http_error_ratio gauge");
        sb.AppendLine($"nexusverde_http_error_ratio{{class=\"4xx\"}} {D(total == 0 ? 0 : (double)erros4xx / total)}");
        sb.AppendLine($"nexusverde_http_error_ratio{{class=\"5xx\"}} {D(total == 0 ? 0 : (double)erros5xx / total)}");

        foreach (var grupo in _contadoresNegocio.GroupBy(x => x.Key.Contains('{') ? x.Key[..x.Key.IndexOf('{')] : x.Key).OrderBy(g => g.Key))
        {
            sb.AppendLine($"# HELP {grupo.Key} {(_ajudaNegocio.TryGetValue(grupo.Key, out var ajuda) ? ajuda : grupo.Key)}");
            sb.AppendLine($"# TYPE {grupo.Key} counter");
            foreach (var linha in grupo.OrderBy(x => x.Key, StringComparer.Ordinal))
                sb.AppendLine($"{linha.Key} {linha.Value}");
        }

        sb.AppendLine("# HELP nexusverde_uptime_seconds Tempo desde a subida do processo, em segundos.");
        sb.AppendLine("# TYPE nexusverde_uptime_seconds gauge");
        sb.AppendLine($"nexusverde_uptime_seconds {D((DateTime.UtcNow - _inicio).TotalSeconds)}");

        return sb.ToString();
    }

    public void Dispose() => _listener.Dispose();
}
