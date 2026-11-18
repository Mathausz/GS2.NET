using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Implementations;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Tests.Unit.Services;

public class TimelineServiceTests
{
    private readonly Mock<ITimelineRepository> _timeline = new();
    private readonly Mock<ISolicitacaoRepository> _solicitacoes = new();
    private readonly TimelineService _service;

    public TimelineServiceTests()
    {
        _service = new TimelineService(_timeline.Object, _solicitacoes.Object, NullLogger<TimelineService>.Instance);
    }

    private SolicitacaoAjuda DadaUmaSolicitacao(Guid cidadaoId, TipoSolicitacao tipo = TipoSolicitacao.SemAgua)
    {
        var solicitacao = TestData.Solicitacao(cidadaoId, Guid.NewGuid(), tipo);
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);
        return solicitacao;
    }

    [Fact]
    public async Task RegistrarAsync_RepositorioOk_GravaEvento()
    {
        // Arrange
        var evento = EventoTimeline.Criar(Guid.NewGuid(), TipoEventoTimeline.Criada, "Criada.");

        // Act
        await _service.RegistrarAsync(evento);

        // Assert
        _timeline.Verify(t => t.AddAsync(evento, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_RepositorioFalha_NaoPropagaExcecao()
    {
        // Arrange
        var evento = EventoTimeline.Criar(Guid.NewGuid(), TipoEventoTimeline.Criada, "Criada.");
        _timeline.Setup(t => t.AddAsync(It.IsAny<EventoTimeline>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Banco fora do ar"));

        // Act
        var erro = await Record.ExceptionAsync(() => _service.RegistrarAsync(evento));

        // Assert
        Assert.Null(erro);
    }

    [Fact]
    public async Task ListarAsync_SolicitacaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _solicitacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitacaoAjuda?)null);

        // Act
        Func<Task> act = () => _service.ListarAsync(TestData.Admin(), Guid.NewGuid(), new TimelineQuery());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ListarAsync_CidadaoQueNaoEAutor_LancaForbiddenException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(Guid.NewGuid());

        // Act
        Func<Task> act = () => _service.ListarAsync(TestData.Cidadao(), solicitacao.Id, new TimelineQuery());

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task ListarAsync_AutorDaSolicitacao_RetornaEventosMapeados()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = DadaUmaSolicitacao(cidadao.Id);
        var query = new TimelineQuery();
        var evento = EventoTimeline.Criar(solicitacao.Id, TipoEventoTimeline.Criada, "Criada.");
        _timeline.Setup(t => t.ListarPorSolicitacaoAsync(solicitacao.Id, query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<EventoTimeline>(new List<EventoTimeline> { evento }, 1, 10, 1));

        // Act
        var pagina = await _service.ListarAsync(cidadao, solicitacao.Id, query);

        // Assert
        Assert.Single(pagina.Items);
        Assert.Equal(TipoEventoTimeline.Criada, pagina.Items[0].Tipo);
    }

    [Fact]
    public async Task ListarAsync_RepositorioFalha_LancaExternalServiceException()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = DadaUmaSolicitacao(cidadao.Id);
        _timeline.Setup(t => t.ListarPorSolicitacaoAsync(It.IsAny<Guid>(), It.IsAny<TimelineQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("sem resposta"));

        // Act
        Func<Task> act = () => _service.ListarAsync(cidadao, solicitacao.Id, new TimelineQuery());

        // Assert
        await Assert.ThrowsAsync<ExternalServiceException>(act);
    }

    [Fact]
    public async Task AdicionarNotaAsync_GestorDoOrgao_GravaEventoDoTipoNota()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(Guid.NewGuid());
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);
        EventoTimeline? gravado = null;
        _timeline.Setup(t => t.AddAsync(It.IsAny<EventoTimeline>(), It.IsAny<CancellationToken>()))
            .Callback<EventoTimeline, CancellationToken>((e, _) => gravado = e);

        // Act
        var resposta = await _service.AdicionarNotaAsync(gestor, solicitacao.Id, new NotaRequest { Texto = "Caminhão-pipa a caminho." });

        // Assert
        Assert.Equal(TipoEventoTimeline.NotaAdicionada, gravado!.Tipo);
        Assert.Equal(gestor.Id, gravado.AutorId);
        Assert.Equal("Caminhão-pipa a caminho.", resposta.Descricao);
    }

    [Fact]
    public async Task AdicionarNotaAsync_Cidadao_LancaForbiddenException()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = DadaUmaSolicitacao(cidadao.Id);

        // Act
        Func<Task> act = () => _service.AdicionarNotaAsync(cidadao, solicitacao.Id, new NotaRequest { Texto = "Minha nota" });

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task AdicionarNotaAsync_GestorDeOutroOrgao_LancaForbiddenException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(Guid.NewGuid(), TipoSolicitacao.AcessoNegado);

        // Act
        Func<Task> act = () => _service.AdicionarNotaAsync(TestData.Gestor(OrgaoResponsavel.Prefeitura),
            solicitacao.Id, new NotaRequest { Texto = "Nota indevida" });

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task AdicionarNotaAsync_SolicitacaoCancelada_LancaDomainException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(Guid.NewGuid());
        solicitacao.Cancelar();

        // Act
        Func<Task> act = () => _service.AdicionarNotaAsync(TestData.Admin(), solicitacao.Id, new NotaRequest { Texto = "Nota tardia" });

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task AdicionarNotaAsync_RepositorioFalha_LancaExternalServiceException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(Guid.NewGuid());
        _timeline.Setup(t => t.AddAsync(It.IsAny<EventoTimeline>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Banco fora do ar"));

        // Act
        Func<Task> act = () => _service.AdicionarNotaAsync(TestData.Admin(), solicitacao.Id, new NotaRequest { Texto = "Nota válida" });

        // Assert
        await Assert.ThrowsAsync<ExternalServiceException>(act);
    }

    [Fact]
    public async Task AdicionarNotaAsync_SolicitacaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _solicitacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitacaoAjuda?)null);

        // Act
        Func<Task> act = () => _service.AdicionarNotaAsync(TestData.Admin(), Guid.NewGuid(), new NotaRequest { Texto = "Nota válida" });

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }
}
