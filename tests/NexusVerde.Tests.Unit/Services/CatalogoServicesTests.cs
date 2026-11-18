using Moq;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Implementations;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Tests.Unit.Services;

public class RegiaoServiceTests
{
    private readonly Mock<IRegiaoRepository> _regioes = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RegiaoService Criar() => new(_regioes.Object, _uow.Object);

    private static RegiaoRequest Pedido() => new()
    {
        Nome = "Sarandi", Municipio = "Porto Alegre", Estado = "RS", Latitude = -30, Longitude = -51, PopulacaoEstimada = 1000
    };

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteERetornaRegiao()
    {
        // Arrange
        Regiao? salva = null;
        _regioes.Setup(r => r.AddAsync(It.IsAny<Regiao>(), It.IsAny<CancellationToken>()))
            .Callback<Regiao, CancellationToken>((r, _) => salva = r);

        // Act
        var resposta = await Criar().CriarAsync(Pedido());

        // Assert
        Assert.Equal("Sarandi", salva!.Nome);
        Assert.Equal("RS", resposta.Estado);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterAsync_RegiaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _regioes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Regiao?)null);

        // Act
        Func<Task> act = () => Criar().ObterAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task AtualizarAsync_RegiaoExistente_AplicaNovosDados()
    {
        // Arrange
        var regiao = TestData.Regiao();
        _regioes.Setup(r => r.GetByIdAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regiao);

        // Act
        var resposta = await Criar().AtualizarAsync(regiao.Id, Pedido());

        // Assert
        Assert.Equal("Sarandi", resposta.Nome);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_RegiaoExistente_RemoveEConfirma()
    {
        // Arrange
        var regiao = TestData.Regiao();
        _regioes.Setup(r => r.GetByIdAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regiao);

        // Act
        await Criar().RemoverAsync(regiao.Id);

        // Assert
        _regioes.Verify(r => r.Remove(regiao), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_RegiaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _regioes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Regiao?)null);

        // Act
        Func<Task> act = () => Criar().RemoverAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ListarAsync_RepositorioComRegioes_MapeiaPaginaParaResposta()
    {
        // Arrange
        var query = new RegiaoQuery { Estado = "RS" };
        _regioes.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Regiao>(new List<Regiao> { TestData.Regiao() }, 1, 10, 1));

        // Act
        var pagina = await Criar().ListarAsync(query);

        // Assert
        Assert.Single(pagina.Items);
        Assert.Equal("Porto Alegre", pagina.Items[0].Municipio);
    }
}

public class OrientacaoServiceTests
{
    private readonly Mock<IOrientacaoRepository> _orientacoes = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private OrientacaoService Criar() => new(_orientacoes.Object, _uow.Object);

    private static OrientacaoRequest Pedido() => new()
    {
        Tipo = TipoSolicitacao.AguaSuspeita, Titulo = "Não beba", Texto = "Ferva por 1 minuto antes de usar.", Ordem = 1
    };

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteOrientacao()
    {
        // Arrange
        Orientacao? salva = null;
        _orientacoes.Setup(r => r.AddAsync(It.IsAny<Orientacao>(), It.IsAny<CancellationToken>()))
            .Callback<Orientacao, CancellationToken>((o, _) => salva = o);

        // Act
        var resposta = await Criar().CriarAsync(Pedido());

        // Assert
        Assert.Equal(TipoSolicitacao.AguaSuspeita, salva!.Tipo);
        Assert.Equal("Não beba", resposta.Titulo);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterAsync_OrientacaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _orientacoes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Orientacao?)null);

        // Act
        Func<Task> act = () => Criar().ObterAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task AtualizarAsync_OrientacaoExistente_AplicaNovoTexto()
    {
        // Arrange
        var orientacao = new Orientacao(TipoSolicitacao.SemAgua, "Antigo", "Texto antigo válido", 1);
        _orientacoes.Setup(r => r.GetByIdAsync(orientacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orientacao);

        // Act
        var resposta = await Criar().AtualizarAsync(orientacao.Id, Pedido());

        // Assert
        Assert.Equal("Não beba", resposta.Titulo);
        Assert.Equal(TipoSolicitacao.AguaSuspeita, resposta.Tipo);
    }

    [Fact]
    public async Task RemoverAsync_OrientacaoExistente_RemoveEConfirma()
    {
        // Arrange
        var orientacao = new Orientacao(TipoSolicitacao.SemAgua, "Título", "Texto válido", 1);
        _orientacoes.Setup(r => r.GetByIdAsync(orientacao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orientacao);

        // Act
        await Criar().RemoverAsync(orientacao.Id);

        // Assert
        _orientacoes.Verify(r => r.Remove(orientacao), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_FiltroPorTipo_RetornaPaginaMapeada()
    {
        // Arrange
        var query = new OrientacaoQuery { Tipo = TipoSolicitacao.SemAgua };
        _orientacoes.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Orientacao>(
                new List<Orientacao> { new(TipoSolicitacao.SemAgua, "Título", "Texto válido", 1) }, 1, 10, 1));

        // Act
        var pagina = await Criar().ListarAsync(query);

        // Assert
        Assert.Single(pagina.Items);
    }
}

public class DesastreServiceTests
{
    private readonly Mock<IDesastreRepository> _desastres = new();
    private readonly Mock<IRegiaoRepository> _regioes = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DesastreService Criar() => new(_desastres.Object, _regioes.Object, _uow.Object);

    [Fact]
    public async Task CriarAsync_RegiaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _regioes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Regiao?)null);

        // Act
        Func<Task> act = () => Criar().CriarAsync(new DesastreRequest
        {
            RegiaoId = Guid.NewGuid(), Tipo = TipoDesastre.Enchente, Gravidade = 3, Descricao = "Alagamento"
        });

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task CriarAsync_DesastreGrave_RecalculaRiscoDaRegiaoParaCritico()
    {
        // Arrange
        var regiao = TestData.Regiao();
        var grave = new Desastre(regiao.Id, TipoDesastre.Enchente, 5, "Catástrofe");
        _regioes.Setup(r => r.GetByIdAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regiao);
        _desastres.Setup(r => r.GetMaisGraveAtivoAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(grave);

        // Act
        await Criar().CriarAsync(new DesastreRequest
        {
            RegiaoId = regiao.Id, Tipo = TipoDesastre.Enchente, Gravidade = 5, Descricao = "Catástrofe"
        });

        // Assert
        Assert.Equal(NivelRisco.Critico, regiao.NivelRisco);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task AtualizarAsync_EncerrandoUnicoDesastre_RegiaoVoltaParaRiscoBaixo()
    {
        // Arrange
        var regiao = TestData.Regiao();
        regiao.DefinirRisco(NivelRisco.Alto);
        var desastre = new Desastre(regiao.Id, TipoDesastre.Enchente, 4, "Alagamento");
        _regioes.Setup(r => r.GetByIdAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regiao);
        _desastres.Setup(r => r.GetByIdAsync(desastre.Id, It.IsAny<CancellationToken>())).ReturnsAsync(desastre);
        _desastres.Setup(r => r.GetMaisGraveAtivoAsync(regiao.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Desastre?)null);

        // Act
        await Criar().AtualizarAsync(desastre.Id, new DesastreUpdateRequest
        {
            Tipo = TipoDesastre.Enchente, Gravidade = 4, Descricao = "Alagamento", Status = StatusDesastre.Encerrado
        });

        // Assert
        Assert.Equal(NivelRisco.Baixo, regiao.NivelRisco);
        Assert.False(desastre.EstaAtivo);
    }

    [Fact]
    public async Task RemoverAsync_DesastreInexistente_LancaNotFoundException()
    {
        // Arrange
        _desastres.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Desastre?)null);

        // Act
        Func<Task> act = () => Criar().RemoverAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }
}

public class PontoAguaServiceTests
{
    private readonly Mock<IPontoAguaRepository> _pontos = new();
    private readonly Mock<IRegiaoRepository> _regioes = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PontoAguaService Criar() => new(_pontos.Object, _regioes.Object, _uow.Object);

    [Fact]
    public async Task ProximosAsync_VariosPontos_OrdenaPorDistanciaERespeitaLimite()
    {
        // Arrange
        var regiaoId = Guid.NewGuid();
        var longe = TestData.Ponto(regiaoId, "A (longe)", -29.0, -51.0);
        var perto = TestData.Ponto(regiaoId, "B (perto)", -30.04, -51.22);
        var medio = TestData.Ponto(regiaoId, "C (médio)", -30.5, -51.5);
        _pontos.Setup(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PontoAgua> { longe, perto, medio });

        // Act
        var resultado = await Criar().ProximosAsync(-30.0346, -51.2177, 2);

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Equal("B (perto)", resultado[0].Ponto.Nome);
        Assert.Equal("C (médio)", resultado[1].Ponto.Nome);
        Assert.True(resultado[0].DistanciaKm < resultado[1].DistanciaKm);
    }

    [Fact]
    public async Task CriarAsync_RegiaoInexistente_LancaNotFoundException()
    {
        // Arrange
        _regioes.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Regiao?)null);

        // Act
        Func<Task> act = () => Criar().CriarAsync(new PontoAguaRequest
        {
            RegiaoId = Guid.NewGuid(), Nome = "Ginásio", Endereco = "Rua A, 1", Latitude = -30, Longitude = -51, CapacidadeLitrosDia = 100
        });

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task AlterarStatusAsync_PontoExistente_PersisteNovoStatus()
    {
        // Arrange
        var ponto = TestData.Ponto(Guid.NewGuid(), "Ginásio", -30, -51);
        _pontos.Setup(r => r.GetByIdAsync(ponto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ponto);

        // Act
        var resposta = await Criar().AlterarStatusAsync(ponto.Id, StatusPontoAgua.Contaminado);

        // Assert
        Assert.Equal(StatusPontoAgua.Contaminado, resposta.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProximosAsync_LimiteAcimaDoMaximo_LimitaEmVinte()
    {
        // Arrange
        var regiaoId = Guid.NewGuid();
        var muitos = Enumerable.Range(0, 30)
            .Select(i => TestData.Ponto(regiaoId, $"P{i}", -30 + i * 0.001, -51))
            .ToList();
        _pontos.Setup(r => r.ListarAtivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(muitos);

        // Act
        var resultado = await Criar().ProximosAsync(-30, -51, 500);

        // Assert
        Assert.Equal(20, resultado.Count);
    }

    [Fact]
    public async Task ObterAsync_PontoInexistente_LancaNotFoundException()
    {
        // Arrange
        _pontos.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((PontoAgua?)null);

        // Act
        Func<Task> act = () => Criar().ObterAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }
}
