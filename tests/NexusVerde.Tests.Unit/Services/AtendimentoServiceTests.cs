using Moq;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Services.Implementations;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Tests.Unit.Services;

public class AtendimentoServiceTests
{
    private readonly Mock<IAtendimentoRepository> _atendimentos = new();
    private readonly Mock<ISolicitacaoRepository> _solicitacoes = new();
    private readonly Mock<IPontoAguaRepository> _pontos = new();
    private readonly Mock<ITimelineService> _timeline = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AtendimentoService _service;

    public AtendimentoServiceTests()
    {
        _service = new AtendimentoService(_atendimentos.Object, _solicitacoes.Object, _pontos.Object,
            _timeline.Object, _uow.Object);
    }

    private SolicitacaoAjuda DadaUmaSolicitacao(TipoSolicitacao tipo = TipoSolicitacao.SemAgua, Guid? pontoSugerido = null)
    {
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid(), tipo, pontoSugerido);
        _solicitacoes.Setup(r => r.GetByIdAsync(solicitacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(solicitacao);
        return solicitacao;
    }

    private PontoAgua DadoUmPonto(StatusPontoAgua status = StatusPontoAgua.Ativo)
    {
        var ponto = TestData.Ponto(Guid.NewGuid(), "Ginásio", -30, -51);
        ponto.AlterarStatus(status);
        _pontos.Setup(r => r.GetByIdAsync(ponto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ponto);
        return ponto;
    }

    private (Atendimento Atendimento, SolicitacaoAjuda Solicitacao, Guid GestorId) DadoUmAtendimentoEmAndamento()
    {
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.AcessoNegado);
        solicitacao.IniciarAtendimento();
        var gestorId = Guid.NewGuid();
        var atendimento = new Atendimento(solicitacao.Id, gestorId, null, null);
        _atendimentos.Setup(r => r.GetByIdAsync(atendimento.Id, It.IsAny<CancellationToken>())).ReturnsAsync(atendimento);
        return (atendimento, solicitacao, gestorId);
    }

    [Fact]
    public async Task CriarAsync_ComPontoAtivo_IniciaAtendimentoERegistraNaTimeline()
    {
        // Arrange
        var ponto = DadoUmPonto();
        var solicitacao = DadaUmaSolicitacao(pontoSugerido: ponto.Id);
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);

        // Act
        var resposta = await _service.CriarAsync(gestor,
            new AtendimentoRequest { SolicitacaoId = solicitacao.Id, Observacao = "Equipe a caminho" });

        // Assert
        Assert.Equal(StatusAtendimento.EmAndamento, resposta.Status);
        Assert.Equal(ponto.Id, resposta.PontoAguaId);
        Assert.Equal(gestor.Id, resposta.GestorId);
        Assert.Equal(StatusSolicitacao.EmAtendimento, solicitacao.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _timeline.Verify(t => t.RegistrarAsync(
            It.Is<EventoTimeline>(e => e.Tipo == TipoEventoTimeline.AtendimentoIniciado),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_SolicitacaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _solicitacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitacaoAjuda?)null);

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Admin(), new AtendimentoRequest { SolicitacaoId = Guid.NewGuid() });

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task CriarAsync_GestorDeOutroOrgao_LancaForbiddenException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.AcessoNegado);

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Gestor(OrgaoResponsavel.Prefeitura),
            new AtendimentoRequest { SolicitacaoId = solicitacao.Id });

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task CriarAsync_GestorDaDefesaCivil_AtendeQualquerOrgao()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.AcessoNegado);

        // Act
        var resposta = await _service.CriarAsync(TestData.Gestor(OrgaoResponsavel.DefesaCivil),
            new AtendimentoRequest { SolicitacaoId = solicitacao.Id });

        // Assert
        Assert.Equal(StatusAtendimento.EmAndamento, resposta.Status);
    }

    [Theory]
    [InlineData(StatusPontoAgua.Lotado)]
    [InlineData(StatusPontoAgua.Contaminado)]
    [InlineData(StatusPontoAgua.Inativo)]
    public async Task CriarAsync_PontoIndisponivel_LancaDomainExceptionESolicitacaoContinuaAberta(StatusPontoAgua status)
    {
        // Arrange
        var ponto = DadoUmPonto(status);
        var solicitacao = DadaUmaSolicitacao();

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Admin(),
            new AtendimentoRequest { SolicitacaoId = solicitacao.Id, PontoAguaId = ponto.Id });

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
        Assert.Equal(StatusSolicitacao.Aberta, solicitacao.Status);
    }

    [Fact]
    public async Task CriarAsync_SemAguaSemPonto_LancaDomainException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.SemAgua);

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Admin(), new AtendimentoRequest { SolicitacaoId = solicitacao.Id });

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task CriarAsync_AcessoNegadoSemPonto_EhPermitido()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.AcessoNegado);

        // Act
        var resposta = await _service.CriarAsync(TestData.Admin(), new AtendimentoRequest { SolicitacaoId = solicitacao.Id });

        // Assert
        Assert.Null(resposta.PontoAguaId);
    }

    [Fact]
    public async Task CriarAsync_SolicitacaoJaEmAtendimento_LancaConflictException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.AcessoNegado);
        solicitacao.IniciarAtendimento();

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Admin(), new AtendimentoRequest { SolicitacaoId = solicitacao.Id });

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
    }

    [Fact]
    public async Task CriarAsync_SolicitacaoCancelada_LancaDomainException()
    {
        // Arrange
        var solicitacao = DadaUmaSolicitacao(TipoSolicitacao.AcessoNegado);
        solicitacao.Cancelar();

        // Act
        Func<Task> act = () => _service.CriarAsync(TestData.Admin(), new AtendimentoRequest { SolicitacaoId = solicitacao.Id });

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task AtualizarStatusAsync_Concluido_MarcaSolicitacaoComoAtendidaERegistraEvento()
    {
        // Arrange
        var (atendimento, solicitacao, gestorId) = DadoUmAtendimentoEmAndamento();

        // Act
        var resposta = await _service.AtualizarStatusAsync(
            TestData.Gestor(OrgaoResponsavel.Policia, gestorId), atendimento.Id,
            new AtendimentoStatusRequest { Status = StatusAtendimento.Concluido, Observacao = "Resolvido" });

        // Assert
        Assert.Equal(StatusAtendimento.Concluido, resposta.Status);
        Assert.Equal(StatusSolicitacao.Atendida, solicitacao.Status);
        _timeline.Verify(t => t.RegistrarAsync(
            It.Is<EventoTimeline>(e => e.Tipo == TipoEventoTimeline.AtendimentoConcluido),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusAsync_Cancelado_ReabreASolicitacao()
    {
        // Arrange
        var (atendimento, solicitacao, gestorId) = DadoUmAtendimentoEmAndamento();

        // Act
        await _service.AtualizarStatusAsync(
            TestData.Gestor(OrgaoResponsavel.Policia, gestorId), atendimento.Id,
            new AtendimentoStatusRequest { Status = StatusAtendimento.Cancelado });

        // Assert
        Assert.Equal(StatusSolicitacao.Aberta, solicitacao.Status);
        _timeline.Verify(t => t.RegistrarAsync(
            It.Is<EventoTimeline>(e => e.Tipo == TipoEventoTimeline.AtendimentoCancelado),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusAsync_OutroGestor_LancaForbiddenException()
    {
        // Arrange
        var (atendimento, _, _) = DadoUmAtendimentoEmAndamento();

        // Act
        Func<Task> act = () => _service.AtualizarStatusAsync(
            TestData.Gestor(OrgaoResponsavel.Policia), atendimento.Id,
            new AtendimentoStatusRequest { Status = StatusAtendimento.Concluido });

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task AtualizarStatusAsync_Admin_PodeAtualizarQualquerAtendimento()
    {
        // Arrange
        var (atendimento, _, _) = DadoUmAtendimentoEmAndamento();

        // Act
        var resposta = await _service.AtualizarStatusAsync(TestData.Admin(), atendimento.Id,
            new AtendimentoStatusRequest { Status = StatusAtendimento.Concluido });

        // Assert
        Assert.Equal(StatusAtendimento.Concluido, resposta.Status);
    }

    [Fact]
    public async Task AtualizarStatusAsync_ParaEmAndamento_LancaDomainException()
    {
        // Arrange
        var (atendimento, _, gestorId) = DadoUmAtendimentoEmAndamento();

        // Act
        Func<Task> act = () => _service.AtualizarStatusAsync(
            TestData.Gestor(OrgaoResponsavel.Policia, gestorId), atendimento.Id,
            new AtendimentoStatusRequest { Status = StatusAtendimento.EmAndamento });

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task ObterAsync_OutroGestor_LancaForbiddenException()
    {
        // Arrange
        var (atendimento, _, _) = DadoUmAtendimentoEmAndamento();

        // Act
        Func<Task> act = () => _service.ObterAsync(TestData.Gestor(OrgaoResponsavel.Prefeitura), atendimento.Id);

        // Assert
        await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Fact]
    public async Task ObterAsync_GestorResponsavel_RetornaAtendimento()
    {
        // Arrange
        var (atendimento, _, gestorId) = DadoUmAtendimentoEmAndamento();

        // Act
        var resposta = await _service.ObterAsync(TestData.Gestor(OrgaoResponsavel.Policia, gestorId), atendimento.Id);

        // Assert
        Assert.Equal(atendimento.Id, resposta.Id);
    }
}
