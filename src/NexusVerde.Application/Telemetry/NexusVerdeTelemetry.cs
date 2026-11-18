using System.Diagnostics;
using System.Diagnostics.Metrics;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Telemetry;

/// <summary>
/// Ponto único de telemetria da aplicação: um <see cref="ActivitySource"/> (tracing distribuído entre as
/// camadas) e um <see cref="Meter"/> (métricas de negócio). Ambos são capturados pelo OpenTelemetry
/// e as métricas também são expostas em /metrics.
/// </summary>
public static class NexusVerdeTelemetry
{
    public const string Name = "NexusVerde";

    public static readonly ActivitySource ActivitySource = new(Name, "1.0.0");
    public static readonly Meter Meter = new(Name, "1.0.0");

    public static readonly Counter<long> SolicitacoesCriadas = Meter.CreateCounter<long>(
        "nexusverde.solicitacoes.criadas", unit: "{solicitacao}",
        description: "Solicitações de ajuda criadas, por tipo e prioridade.");

    public static readonly Counter<long> AtendimentosFinalizados = Meter.CreateCounter<long>(
        "nexusverde.atendimentos.finalizados", unit: "{atendimento}",
        description: "Atendimentos concluídos ou cancelados.");

    public static readonly Counter<long> LoginsFalhos = Meter.CreateCounter<long>(
        "nexusverde.auth.logins_falhos", unit: "{login}",
        description: "Tentativas de login com credenciais inválidas.");

    public static readonly Counter<long> IdempotencyReplays = Meter.CreateCounter<long>(
        "nexusverde.idempotency.replays", unit: "{requisicao}",
        description: "Requisições repetidas atendidas pela resposta guardada (Idempotency-Key).");

    public static readonly Counter<long> RateLimitRejeitados = Meter.CreateCounter<long>(
        "nexusverde.ratelimit.rejeitados", unit: "{requisicao}",
        description: "Requisições rejeitadas com HTTP 429.");

    public static readonly Counter<long> TimelineFalhas = Meter.CreateCounter<long>(
        "nexusverde.timeline.falhas", unit: "{evento}",
        description: "Eventos que não puderam ser gravados na linha do tempo.");

    public static void SolicitacaoCriada(TipoSolicitacao tipo, NivelPrioridade prioridade) =>
        SolicitacoesCriadas.Add(1,
            new KeyValuePair<string, object?>("tipo", tipo.ToString()),
            new KeyValuePair<string, object?>("prioridade", prioridade.ToString()));

    public static void AtendimentoFinalizado(StatusAtendimento status) =>
        AtendimentosFinalizados.Add(1, new KeyValuePair<string, object?>("status", status.ToString()));
}
