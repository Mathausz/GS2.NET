using NexusVerde.Application.Common;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Queries;

/// <summary>Filtros de regiões. SortBy: nome, municipio, estado, risco, populacao, criadoEm.</summary>
public class RegiaoQuery : QueryParameters
{
    public string? Busca { get; set; }
    public string? Estado { get; set; }
    public NivelRisco? NivelRisco { get; set; }
}

/// <summary>Filtros de desastres. SortBy: gravidade, tipo, status, inicio.</summary>
public class DesastreQuery : QueryParameters
{
    public Guid? RegiaoId { get; set; }
    public TipoDesastre? Tipo { get; set; }
    public StatusDesastre? Status { get; set; }
}

/// <summary>Filtros de pontos de água. SortBy: nome, capacidade, status.</summary>
public class PontoAguaQuery : QueryParameters
{
    public string? Busca { get; set; }
    public Guid? RegiaoId { get; set; }
    public StatusPontoAgua? Status { get; set; }
}

/// <summary>Filtros de solicitações. SortBy: score, criadoEm, status, tipo.</summary>
public class SolicitacaoQuery : QueryParameters
{
    public Guid? RegiaoId { get; set; }
    public TipoSolicitacao? Tipo { get; set; }
    public StatusSolicitacao? Status { get; set; }
    public NivelPrioridade? Prioridade { get; set; }
    /// <summary>Somente Admin e Defesa Civil podem filtrar por órgão.</summary>
    public OrgaoResponsavel? OrgaoDestino { get; set; }
}

/// <summary>Filtros de atendimentos. SortBy: iniciadoEm, status.</summary>
public class AtendimentoQuery : QueryParameters
{
    public Guid? SolicitacaoId { get; set; }
    public StatusAtendimento? Status { get; set; }
}

/// <summary>Filtros de orientações. SortBy: ordem, titulo.</summary>
public class OrientacaoQuery : QueryParameters
{
    public TipoSolicitacao? Tipo { get; set; }
}

/// <summary>Filtros de usuários. SortBy: nome, email, perfil.</summary>
public class UsuarioQuery : QueryParameters
{
    public string? Busca { get; set; }
    public PerfilUsuario? Perfil { get; set; }
}

/// <summary>Filtros da linha do tempo. Ordem padrão: do mais antigo ao mais novo (use desc=true para inverter).</summary>
public class TimelineQuery : QueryParameters
{
    public TipoEventoTimeline? Tipo { get; set; }
}
