using NexusVerde.Application.DTOs;
using NexusVerde.Application.Hateoas;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Tests.Unit.Application;

public class ResourceLinksTests
{
    private static SolicitacaoResponse Solicitacao(Guid cidadaoId, StatusSolicitacao status = StatusSolicitacao.Aberta,
        OrgaoResponsavel orgao = OrgaoResponsavel.Prefeitura, Guid? ponto = null) =>
        new(Guid.NewGuid(), cidadaoId, Guid.NewGuid(), null, ponto, TipoSolicitacao.SemAgua, "Sem água", 4, 10,
            -30, -51, 40, NivelPrioridade.Media, orgao, status, DateTime.UtcNow);

    private static bool Tem(IReadOnlyList<LinkDto>? links, string rel) => links!.Any(l => l.Rel == rel);

    [Fact]
    public void Com_SolicitacaoAbertaDoCidadaoAutor_IncluiCancelarETimeline()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = Solicitacao(cidadao.Id);

        // Act
        var resposta = ResourceLinks.Com(solicitacao, cidadao);

        // Assert
        Assert.True(Tem(resposta.Links, "self"));
        Assert.True(Tem(resposta.Links, "timeline"));
        Assert.True(Tem(resposta.Links, "cancelar"));
        Assert.False(Tem(resposta.Links, "assumir-atendimento"));
    }

    [Fact]
    public void Com_SolicitacaoEmAtendimentoDoCidadaoAutor_NaoIncluiCancelar()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = Solicitacao(cidadao.Id, StatusSolicitacao.EmAtendimento);

        // Act
        var resposta = ResourceLinks.Com(solicitacao, cidadao);

        // Assert
        Assert.False(Tem(resposta.Links, "cancelar"));
    }

    [Fact]
    public void Com_SolicitacaoAbertaParaGestorDoOrgao_IncluiAssumirENota()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);
        var solicitacao = Solicitacao(Guid.NewGuid(), orgao: OrgaoResponsavel.Prefeitura);

        // Act
        var resposta = ResourceLinks.Com(solicitacao, gestor);

        // Assert
        Assert.True(Tem(resposta.Links, "assumir-atendimento"));
        Assert.True(Tem(resposta.Links, "adicionar-nota"));
        Assert.False(Tem(resposta.Links, "cancelar"));
    }

    [Fact]
    public void Com_SolicitacaoDeOutroOrgaoParaGestor_NaoIncluiAcoes()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);
        var solicitacao = Solicitacao(Guid.NewGuid(), orgao: OrgaoResponsavel.Policia);

        // Act
        var resposta = ResourceLinks.Com(solicitacao, gestor);

        // Assert
        Assert.False(Tem(resposta.Links, "assumir-atendimento"));
        Assert.False(Tem(resposta.Links, "adicionar-nota"));
    }

    [Fact]
    public void Com_SolicitacaoAtendidaParaGestor_NaoIncluiAssumirNemNota()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);
        var solicitacao = Solicitacao(Guid.NewGuid(), StatusSolicitacao.Atendida);

        // Act
        var resposta = ResourceLinks.Com(solicitacao, gestor);

        // Assert
        Assert.False(Tem(resposta.Links, "assumir-atendimento"));
        Assert.False(Tem(resposta.Links, "adicionar-nota"));
    }

    [Fact]
    public void Com_SolicitacaoComPontoSugerido_IncluiLinkDoPonto()
    {
        // Arrange
        var pontoId = Guid.NewGuid();
        var solicitacao = Solicitacao(Guid.NewGuid(), ponto: pontoId);

        // Act
        var resposta = ResourceLinks.Com(solicitacao, TestData.Admin());

        // Assert
        Assert.Contains(resposta.Links!, l => l.Rel == "ponto-sugerido" && l.Href.EndsWith(pontoId.ToString()));
    }

    [Fact]
    public void Com_RegiaoParaAdmin_IncluiAtualizarERemover()
    {
        // Arrange
        var regiao = new RegiaoResponse(Guid.NewGuid(), "Centro", "POA", "RS", -30, -51, 100, NivelRisco.Baixo, DateTime.UtcNow);

        // Act
        var comoAdmin = ResourceLinks.Com(regiao, TestData.Admin());
        var comoCidadao = ResourceLinks.Com(regiao, TestData.Cidadao());

        // Assert
        Assert.True(Tem(comoAdmin.Links, "atualizar"));
        Assert.True(Tem(comoAdmin.Links, "remover"));
        Assert.False(Tem(comoCidadao.Links, "atualizar"));
        Assert.True(Tem(comoCidadao.Links, "desastres"));
    }

    [Fact]
    public void Com_OrientacaoParaAnonimo_IncluiApenasLeitura()
    {
        // Arrange
        var orientacao = new OrientacaoResponse(Guid.NewGuid(), TipoSolicitacao.SemAgua, "Título", "Texto", 1);

        // Act
        var resposta = ResourceLinks.Com(orientacao, null);

        // Assert
        Assert.True(Tem(resposta.Links, "self"));
        Assert.False(Tem(resposta.Links, "atualizar"));
    }

    [Fact]
    public void Com_PontoAguaParaGestor_IncluiAlterarStatusMasNaoRemover()
    {
        // Arrange
        var ponto = new PontoAguaResponse(Guid.NewGuid(), Guid.NewGuid(), "Ginásio", "Rua A", -30, -51, 100,
            StatusPontoAgua.Ativo, null);

        // Act
        var resposta = ResourceLinks.Com(ponto, TestData.Gestor(OrgaoResponsavel.Prefeitura));

        // Assert
        Assert.True(Tem(resposta.Links, "alterar-status"));
        Assert.False(Tem(resposta.Links, "remover"));
    }

    [Fact]
    public void Com_AtendimentoEmAndamentoDoGestorResponsavel_IncluiConcluir()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);
        var atendimento = new AtendimentoResponse(Guid.NewGuid(), Guid.NewGuid(), gestor.Id, null,
            StatusAtendimento.EmAndamento, null, DateTime.UtcNow, null);

        // Act
        var resposta = ResourceLinks.Com(atendimento, gestor);

        // Assert
        Assert.True(Tem(resposta.Links, "concluir"));
    }

    [Fact]
    public void Com_AtendimentoConcluido_NaoIncluiAcoes()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);
        var atendimento = new AtendimentoResponse(Guid.NewGuid(), Guid.NewGuid(), gestor.Id, null,
            StatusAtendimento.Concluido, null, DateTime.UtcNow, DateTime.UtcNow);

        // Act
        var resposta = ResourceLinks.Com(atendimento, gestor);

        // Assert
        Assert.False(Tem(resposta.Links, "concluir"));
    }

    [Fact]
    public void Com_UsuarioInativoParaAdmin_NaoIncluiDesativar()
    {
        // Arrange
        var usuario = new UsuarioResponse(Guid.NewGuid(), "Ana", "ana@x.com", PerfilUsuario.Cidadao,
            OrgaoResponsavel.Nenhum, false);

        // Act
        var resposta = ResourceLinks.Com(usuario, TestData.Admin());

        // Assert
        Assert.False(Tem(resposta.Links, "desativar"));
    }

    [Fact]
    public void Com_EventoDaTimeline_IncluiLinkDaSolicitacao()
    {
        // Arrange
        var solicitacaoId = Guid.NewGuid();
        var evento = new TimelineEventoResponse("abc", solicitacaoId, TipoEventoTimeline.Criada, "Criada.", null, null,
            null, new Dictionary<string, string>(), DateTime.UtcNow);

        // Act
        var resposta = ResourceLinks.Com(evento);

        // Assert
        Assert.Contains(resposta.Links!, l => l.Rel == "solicitacao" && l.Href.EndsWith(solicitacaoId.ToString()));
    }

    [Fact]
    public void Pagina_PaginaDoMeio_IncluiPrevENextPreservandoFiltros()
    {
        // Arrange
        var consulta = new Dictionary<string, string> { ["estado"] = "RS", ["page"] = "2", ["pageSize"] = "10" };

        // Act
        var links = ResourceLinks.Pagina("/api/v1/regioes", consulta, 2, 10, 5);

        // Assert
        Assert.True(Tem(links, "prev"));
        Assert.True(Tem(links, "next"));
        var next = links.Single(l => l.Rel == "next");
        Assert.Contains("estado=RS", next.Href);
        Assert.Contains("page=3", next.Href);
        Assert.Single(next.Href.Split("page=").Skip(1).Where(x => !x.StartsWith("Size")));
    }

    [Fact]
    public void Pagina_PrimeiraEUnicaPagina_NaoIncluiPrevNemNext()
    {
        // Arrange
        var consulta = new Dictionary<string, string>();

        // Act
        var links = ResourceLinks.Pagina("/api/v1/regioes", consulta, 1, 10, 1);

        // Assert
        Assert.False(Tem(links, "prev"));
        Assert.False(Tem(links, "next"));
        Assert.True(Tem(links, "first"));
        Assert.True(Tem(links, "last"));
    }

    [Fact]
    public void Pagina_SemResultados_UltimaPaginaEhUm()
    {
        // Arrange & Act
        var links = ResourceLinks.Pagina("/api/v1/regioes", new Dictionary<string, string>(), 1, 10, 0);

        // Assert
        Assert.Contains("page=1", links.Single(l => l.Rel == "last").Href);
    }
}
