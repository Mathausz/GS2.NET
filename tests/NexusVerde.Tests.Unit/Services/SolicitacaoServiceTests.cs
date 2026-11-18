using Moq;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Implementations;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Tests.Unit.Services;

public class SolicitacaoServiceTests
{
    private readonly Mock<ISolicitacaoRepository> _solicitacoes = new();
    private readonly Mock<IRegiaoRepository> _regioes = new();
    private readonly Mock<IDesastreRepository> _desastres = new();
    private readonly Mock<IPontoAguaRepository> _pontos = new();
    private readonly Mock<IOrientacaoRepository> _orientacoes = new();
    private readonly Mock<IWeatherClient> _weather = new();
    private readonly Mock<ITimelineService> _timeline = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly SolicitacaoService _service;
    private SolicitacaoAjuda? _salva;

    public SolicitacaoServiceTests()
    {
        _solicitacoes.Setup(r => r.AddAsync(It.IsAny<SolicitacaoAjuda>(), It.IsAny<CancellationToken>()))
            .Callback<SolicitacaoAjuda, CancellationToken>((s, _) => _salva = s);
        _pontos.Setup(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PontoAgua>());
        _orientacoes.Setup(r => r.ListarPorTipoAsync(It.IsAny<TipoSolicitacao>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Orientacao>());
        _weather.Setup(w => w.ObterPrevisaoAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClimaPrevisto?)null);

        _service = new SolicitacaoService(_solicitacoes.Object, _regioes.Object, _desastres.Object,
            _pontos.Object, _orientacoes.Object, _weather.Object, _timeline.Object, _uow.Object);
    }

    private static SolicitacaoRequest Pedido(Guid regiaoId, TipoSolicitacao tipo = TipoSolicitacao.SemAgua) => new()
    {
        RegiaoId = regiaoId, Tipo = tipo, Descricao = "Bairro sem água desde ontem", QuantidadePessoas = 10, HorasSemAgua = 24
    };

    private Regiao DadaUmaRegiao(double lat = -30.0346, double lng = -51.2177)
    {
        var regiao = TestData.Regiao(lat, lng);
        _regioes.Setup(r => r.GetByIdAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regiao);
        return regiao;
    }

    [Fact]
    public async Task CriarAsync_RegiaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _regioes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Regiao?)null);

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Cidadao(), Pedido(Guid.NewGuid()));

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task CriarAsync_VariosPontosAtivos_IndicaOMaisProximoEDevolveOrientacoes()
    {
        // Arrange
        var regiao = DadaUmaRegiao();
        var perto = TestData.Ponto(regiao.Id, "Perto", -30.0350, -51.2180);
        var longe = TestData.Ponto(regiao.Id, "Longe", -29.5000, -51.0000);
        _pontos.Setup(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PontoAgua> { longe, perto });
        _orientacoes.Setup(r => r.ListarPorTipoAsync(TipoSolicitacao.SemAgua, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Orientacao> { new(TipoSolicitacao.SemAgua, "Vá ao ponto", "Leve recipientes limpos", 1) });
        var cidadao = TestData.Cidadao();

        // Act
        var resposta = await _service.CriarAsync(cidadao, Pedido(regiao.Id));

        // Assert
        Assert.Equal("Perto", resposta.PontoMaisProximo!.Ponto.Nome);
        Assert.Equal(perto.Id, resposta.Solicitacao.PontoSugeridoId);
        Assert.Single(resposta.Orientacoes);
        Assert.Equal(cidadao.Id, _salva!.CidadaoId);
        Assert.Equal(StatusSolicitacao.Aberta, _salva.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_SemPontosAtivos_NaoFalhaESemPontoSugerido()
    {
        // Arrange
        var regiao = DadaUmaRegiao();

        // Act
        var resposta = await _service.CriarAsync(TestData.Cidadao(), Pedido(regiao.Id));

        // Assert
        Assert.Null(resposta.PontoMaisProximo);
        Assert.Null(resposta.Solicitacao.PontoSugeridoId);
    }

    [Fact]
    public async Task CriarAsync_DesastreGraveEChuvaForte_CalculaScoreComTodosOsComponentes()
    {
        // Arrange: 50 (gravidade 5) + 2 (10 pessoas) + 6,7 (24h) + 10 (chuva 50mm) = 68,7
        var regiao = DadaUmaRegiao();
        var desastre = new Desastre(regiao.Id, TipoDesastre.Enchente, 5, "Enchente grave");
        _desastres.Setup(r => r.GetMaisGraveAtivoAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(desastre);
        _weather.Setup(w => w.ObterPrevisaoAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClimaPrevisto(50));

        // Act
        var resposta = await _service.CriarAsync(TestData.Cidadao(), Pedido(regiao.Id));

        // Assert
        Assert.Equal(68.7, resposta.Solicitacao.ScorePrioridade, 1);
        Assert.Equal(NivelPrioridade.Alta, resposta.Solicitacao.NivelPrioridade);
        Assert.Equal(desastre.Id, resposta.Solicitacao.DesastreId);
    }

    [Fact]
    public async Task CriarAsync_ServicoDeClimaIndisponivel_SegueSemComponenteDeChuva()
    {
        // Arrange: 0 (sem desastre) + 2 (10 pessoas) + 6,7 (24h) = 8,7
        var regiao = DadaUmaRegiao();

        // Act
        var resposta = await _service.CriarAsync(TestData.Cidadao(), Pedido(regiao.Id));

        // Assert
        Assert.Equal(8.7, resposta.Solicitacao.ScorePrioridade, 1);
        Assert.Equal(NivelPrioridade.Baixa, resposta.Solicitacao.NivelPrioridade);
    }

    [Theory]
    [InlineData(TipoSolicitacao.AcessoNegado, OrgaoResponsavel.Policia)]
    [InlineData(TipoSolicitacao.AguaSuspeita, OrgaoResponsavel.DefesaCivil)]
    [InlineData(TipoSolicitacao.SemAgua, OrgaoResponsavel.Prefeitura)]
    public async Task CriarAsync_ConformeTipo_EncaminhaAoOrgaoCorreto(TipoSolicitacao tipo, OrgaoResponsavel esperado)
    {
        // Arrange
        var regiao = DadaUmaRegiao();

        // Act
        var resposta = await _service.CriarAsync(TestData.Cidadao(), Pedido(regiao.Id, tipo));

        // Assert
        Assert.Equal(esperado, resposta.Solicitacao.OrgaoDestino);
    }

    [Fact]
    public async Task CriarAsync_SemCoordenadasNoPedido_UsaCentroDaRegiao()
    {
        // Arrange
        var regiao = DadaUmaRegiao(-30.5, -51.5);

        // Act
        var resposta = await _service.CriarAsync(TestData.Cidadao(), Pedido(regiao.Id));

        // Assert
        Assert.Equal(-30.5, resposta.Solicitacao.Latitude);
        Assert.Equal(-51.5, resposta.Solicitacao.Longitude);
    }

    [Fact]
    public async Task CriarAsync_SolicitacaoCriada_RegistraEventoCriadaNaTimeline()
    {
        // Arrange
        var regiao = DadaUmaRegiao();

        // Act
        var resposta = await _service.CriarAsync(TestData.Cidadao(), Pedido(regiao.Id));

        // Assert
        _timeline.Verify(t => t.RegistrarAsync(
            It.Is<EventoTimeline>(e => e.Tipo == TipoEventoTimeline.Criada && e.SolicitacaoId == resposta.Solicitacao.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterAsync_CidadaoDeOutroAutor_LancaForbiddenException()
    {
        // Arrange
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid());
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);

        // Act
        Func<Task> act = () => _service.ObterAsync(TestData.Cidadao(), solicitacao.Id);

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task ObterAsync_Autor_RetornaSolicitacao()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = TestData.Solicitacao(cidadao.Id, Guid.NewGuid());
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);

        // Act
        var resposta = await _service.ObterAsync(cidadao, solicitacao.Id);

        // Assert
        Assert.Equal(solicitacao.Id, resposta.Id);
    }

    [Fact]
    public async Task ObterAsync_GestorDeOutroOrgao_LancaForbiddenException()
    {
        // Arrange
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid(), TipoSolicitacao.AcessoNegado);
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);

        // Act
        Func<Task> act = () => _service.ObterAsync(TestData.Gestor(OrgaoResponsavel.Prefeitura), solicitacao.Id);

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task ObterAsync_SolicitacaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _solicitacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitacaoAjuda?)null);

        // Act
        Func<Task> act = () => _service.ObterAsync(TestData.Admin(), Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task CancelarAsync_OutroCidadao_LancaForbiddenException()
    {
        // Arrange
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid());
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);

        // Act
        Func<Task> act = () => _service.CancelarAsync(TestData.Cidadao(), solicitacao.Id);

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task CancelarAsync_PeloAutor_MudaStatusERegistraEvento()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = TestData.Solicitacao(cidadao.Id, Guid.NewGuid());
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);

        // Act
        var resposta = await _service.CancelarAsync(cidadao, solicitacao.Id);

        // Assert
        Assert.Equal(StatusSolicitacao.Cancelada, resposta.Status);
        _timeline.Verify(t => t.RegistrarAsync(
            It.Is<EventoTimeline>(e => e.Tipo == TipoEventoTimeline.Cancelada),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFilaAsync_GestorDePolicia_RestringeAoProprioOrgao()
    {
        // Arrange
        var query = new SolicitacaoQuery();
        _solicitacoes.Setup(r => r.GetPagedAsync(query, null, OrgaoResponsavel.Policia, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SolicitacaoAjuda>(new List<SolicitacaoAjuda>(), 1, 10, 0));

        // Act
        var resultado = await _service.ListarFilaAsync(TestData.Gestor(OrgaoResponsavel.Policia), query);

        // Assert
        Assert.Empty(resultado.Items);
        _solicitacoes.Verify(r => r.GetPagedAsync(query, null, OrgaoResponsavel.Policia, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFilaAsync_Admin_NaoAplicaRestricaoDeOrgao()
    {
        // Arrange
        var query = new SolicitacaoQuery();
        _solicitacoes.Setup(r => r.GetPagedAsync(query, null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SolicitacaoAjuda>(new List<SolicitacaoAjuda>(), 1, 10, 0));

        // Act
        await _service.ListarFilaAsync(TestData.Admin(), query);

        // Assert
        _solicitacoes.Verify(r => r.GetPagedAsync(query, null, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarMinhasAsync_Cidadao_FiltraPeloIdDoAutor()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var query = new SolicitacaoQuery();
        _solicitacoes.Setup(r => r.GetPagedAsync(query, cidadao.Id, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SolicitacaoAjuda>(
                new List<SolicitacaoAjuda> { TestData.Solicitacao(cidadao.Id, Guid.NewGuid()) }, 1, 10, 1));

        // Act
        var pagina = await _service.ListarMinhasAsync(cidadao, query);

        // Assert
        Assert.Single(pagina.Items);
    }
}
