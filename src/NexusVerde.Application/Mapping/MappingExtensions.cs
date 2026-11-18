using NexusVerde.Application.DTOs;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Mapping;

/// <summary>Conversões manuais entidade → DTO (explícitas e fáceis de testar).</summary>
public static class MappingExtensions
{
    public static UsuarioResponse ToResponse(this Usuario u) =>
        new(u.Id, u.Nome, u.Email, u.Perfil, u.Orgao, u.Ativo);

    public static RegiaoResponse ToResponse(this Regiao r) =>
        new(r.Id, r.Nome, r.Municipio, r.Estado, r.Latitude, r.Longitude, r.PopulacaoEstimada, r.NivelRisco, r.CriadoEm);

    public static DesastreResponse ToResponse(this Desastre d) =>
        new(d.Id, d.RegiaoId, d.Tipo, d.Gravidade, d.Descricao, d.Status, d.IniciadoEm, d.EncerradoEm);

    public static PontoAguaResponse ToResponse(this PontoAgua p) =>
        new(p.Id, p.RegiaoId, p.Nome, p.Endereco, p.Latitude, p.Longitude, p.CapacidadeLitrosDia, p.Status, p.Descricao);

    public static SolicitacaoResponse ToResponse(this SolicitacaoAjuda s) =>
        new(s.Id, s.CidadaoId, s.RegiaoId, s.DesastreId, s.PontoSugeridoId, s.Tipo, s.Descricao,
            s.QuantidadePessoas, s.HorasSemAgua, s.Latitude, s.Longitude, s.ScorePrioridade,
            s.NivelPrioridade, s.OrgaoDestino, s.Status, s.CriadoEm);

    public static AtendimentoResponse ToResponse(this Atendimento a) =>
        new(a.Id, a.SolicitacaoId, a.GestorId, a.PontoAguaId, a.Status, a.Observacao, a.IniciadoEm, a.FinalizadoEm);

    public static OrientacaoResponse ToResponse(this Orientacao o) =>
        new(o.Id, o.Tipo, o.Titulo, o.Texto, o.Ordem);

    public static TimelineEventoResponse ToResponse(this EventoTimeline e) =>
        new(e.Id, e.SolicitacaoId, e.Tipo, e.Descricao, e.AutorId, e.AutorPerfil, e.Orgao, e.Dados, e.OcorridoEm);
}
