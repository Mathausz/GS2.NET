using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Domain.Entities;

/// <summary>
/// Evento da linha do tempo de uma solicitação (tabela NV_TIMELINE). É append-only: cada mudança relevante
/// (criação, atendimento, cancelamento, nota do gestor) vira um registro; o campo <see cref="Dados"/>
/// guarda detalhes de formato livre (score, nível, ponto, órgão...) serializados em JSON.
/// </summary>
public sealed class EventoTimeline
{
    private static long _ultimoTick;

    /// <summary>Identificador do evento (GUID em texto), atribuído na criação.</summary>
    public string Id { get; set; } = string.Empty;

    public Guid SolicitacaoId { get; init; }
    public TipoEventoTimeline Tipo { get; init; }
    public string Descricao { get; init; } = string.Empty;
    public Guid? AutorId { get; init; }
    public PerfilUsuario? AutorPerfil { get; init; }
    public OrgaoResponsavel? Orgao { get; init; }
    public IReadOnlyDictionary<string, string> Dados { get; init; } = new Dictionary<string, string>();
    public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;

    public static EventoTimeline Criar(Guid solicitacaoId, TipoEventoTimeline tipo, string descricao,
        Guid? autorId = null, PerfilUsuario? autorPerfil = null, OrgaoResponsavel? orgao = null,
        IDictionary<string, string>? dados = null)
    {
        if (solicitacaoId == Guid.Empty)
            throw new DomainException("A solicitação do evento é obrigatória.");

        return new EventoTimeline
        {
            Id = Guid.NewGuid().ToString("D"),
            SolicitacaoId = solicitacaoId,
            Tipo = tipo,
            Descricao = Guard.NotEmpty(descricao, "Descrição", 1000),
            AutorId = autorId,
            AutorPerfil = autorPerfil,
            Orgao = orgao,
            Dados = dados is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(dados),
            OcorridoEm = AgoraMonotonico()
        };
    }

    /// <summary>
    /// UtcNow estritamente crescente: dois eventos criados no mesmo instante recebem horários distintos
    /// (1 tick = 100 ns), garantindo a ordem cronológica da linha do tempo.
    /// </summary>
    private static DateTime AgoraMonotonico()
    {
        while (true)
        {
            var ultimo = Interlocked.Read(ref _ultimoTick);
            var agora = Math.Max(DateTime.UtcNow.Ticks, ultimo + 1);
            if (Interlocked.CompareExchange(ref _ultimoTick, agora, ultimo) == ultimo)
                return new DateTime(agora, DateTimeKind.Utc);
        }
    }
}
