using NexusVerde.Application.Common;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Tests.Unit;

/// <summary>Fábricas de dados controlados, compartilhadas pelos testes unitários.</summary>
internal static class TestData
{
    public static Regiao Regiao(double lat = -30.0346, double lng = -51.2177) =>
        new("Centro", "Porto Alegre", "RS", lat, lng, 1000);

    public static PontoAgua Ponto(Guid regiaoId, string nome, double lat, double lng) =>
        new(regiaoId, nome, "Rua Teste, 1", lat, lng, 5000);

    public static UsuarioContexto Cidadao(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), PerfilUsuario.Cidadao, OrgaoResponsavel.Nenhum);

    public static UsuarioContexto Gestor(OrgaoResponsavel orgao, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), PerfilUsuario.Gestor, orgao);

    public static UsuarioContexto Admin(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), PerfilUsuario.Admin, OrgaoResponsavel.Nenhum);

    public static SolicitacaoAjuda Solicitacao(Guid cidadaoId, Guid regiaoId, TipoSolicitacao tipo = TipoSolicitacao.SemAgua,
        Guid? pontoSugerido = null) =>
        new(cidadaoId, regiaoId, tipo, "Sem água há horas", 4, 10, -30.03, -51.21, 40,
            NivelPrioridade.Media, PrioridadeCalculator.OrgaoDestinoPorTipo(tipo), null, pontoSugerido);
}
