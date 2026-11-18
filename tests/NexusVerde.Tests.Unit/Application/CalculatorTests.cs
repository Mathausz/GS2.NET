using NexusVerde.Application.Common;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Tests.Unit.Application;

public class PrioridadeCalculatorTests
{
    [Fact]
    public void Calcular_ComDadosMinimos_RetornaPrioridadeBaixa()
    {
        // Arrange & Act
        var resultado = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 0, 1, 0, null);

        // Assert
        Assert.Equal(0.2, resultado.Score, 1);
        Assert.Equal(NivelPrioridade.Baixa, resultado.Nivel);
    }

    [Fact]
    public void Calcular_TodosOsComponentes_SomaCorretamente()
    {
        // Arrange: 3*10 (gravidade) + 50*0,2 (pessoas) + 36/72*20 (tempo) + 25/50*10 (chuva) = 55

        // Act
        var resultado = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 3, 50, 36, 25);

        // Assert
        Assert.Equal(55.0, resultado.Score, 1);
        Assert.Equal(NivelPrioridade.Alta, resultado.Nivel);
    }

    [Fact]
    public void Calcular_AcessoNegado_AdicionaQuinzePontosEmRelacaoASemAgua()
    {
        // Arrange
        var semAgua = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 2, 10, 12, null);

        // Act
        var negado = PrioridadeCalculator.Calcular(TipoSolicitacao.AcessoNegado, 2, 10, 12, null);

        // Assert
        Assert.Equal(15.0, negado.Score - semAgua.Score, 1);
    }

    [Fact]
    public void Calcular_AguaSuspeita_AdicionaDezPontosEmRelacaoASemAgua()
    {
        // Arrange
        var semAgua = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 2, 10, 12, null);

        // Act
        var suspeita = PrioridadeCalculator.Calcular(TipoSolicitacao.AguaSuspeita, 2, 10, 12, null);

        // Assert
        Assert.Equal(10.0, suspeita.Score - semAgua.Score, 1);
    }

    [Fact]
    public void Calcular_ValoresExtremos_LimitaScoreEmCem()
    {
        // Arrange & Act
        var resultado = PrioridadeCalculator.Calcular(TipoSolicitacao.AcessoNegado, 5, 100_000, 10_000, 500);

        // Assert
        Assert.Equal(100.0, resultado.Score);
        Assert.Equal(NivelPrioridade.Critica, resultado.Nivel);
    }

    [Fact]
    public void Calcular_SemDadoDeClima_EquivaleAChuvaZero()
    {
        // Arrange
        var semClima = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 2, 10, 12, null);

        // Act
        var chuvaZero = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 2, 10, 12, 0);

        // Assert
        Assert.Equal(semClima.Score, chuvaZero.Score);
    }

    [Fact]
    public void Calcular_ChuvaForte_AumentaOScore()
    {
        // Arrange
        var seco = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 2, 10, 12, 0);

        // Act
        var chuvoso = PrioridadeCalculator.Calcular(TipoSolicitacao.SemAgua, 2, 10, 12, 50);

        // Assert
        Assert.True(chuvoso.Score > seco.Score);
    }

    [Theory]
    [InlineData(0, NivelPrioridade.Baixa)]
    [InlineData(29.9, NivelPrioridade.Baixa)]
    [InlineData(30, NivelPrioridade.Media)]
    [InlineData(54.9, NivelPrioridade.Media)]
    [InlineData(55, NivelPrioridade.Alta)]
    [InlineData(74.9, NivelPrioridade.Alta)]
    [InlineData(75, NivelPrioridade.Critica)]
    [InlineData(100, NivelPrioridade.Critica)]
    public void Classificar_ConformeFaixa_RetornaNivelEsperado(double score, NivelPrioridade esperado)
    {
        // Arrange & Act
        var nivel = PrioridadeCalculator.Classificar(score);

        // Assert
        Assert.Equal(esperado, nivel);
    }

    [Theory]
    [InlineData(TipoSolicitacao.AcessoNegado, OrgaoResponsavel.Policia)]
    [InlineData(TipoSolicitacao.AguaSuspeita, OrgaoResponsavel.DefesaCivil)]
    [InlineData(TipoSolicitacao.SemAgua, OrgaoResponsavel.Prefeitura)]
    public void OrgaoDestinoPorTipo_ConformeTipo_RoteiaParaOrgaoCorreto(TipoSolicitacao tipo, OrgaoResponsavel esperado)
    {
        // Arrange & Act
        var orgao = PrioridadeCalculator.OrgaoDestinoPorTipo(tipo);

        // Assert
        Assert.Equal(esperado, orgao);
    }
}

public class GeoCalculatorTests
{
    [Fact]
    public void DistanciaKm_MesmoPonto_RetornaZero()
    {
        // Arrange & Act
        var distancia = GeoCalculator.DistanciaKm(-30.03, -51.21, -30.03, -51.21);

        // Assert
        Assert.Equal(0, distancia, 6);
    }

    [Fact]
    public void DistanciaKm_SaoPauloRio_RetornaCercaDe357Km()
    {
        // Arrange & Act
        var distancia = GeoCalculator.DistanciaKm(-23.5505, -46.6333, -22.9068, -43.1729);

        // Assert
        Assert.InRange(distancia, 350, 365);
    }

    [Fact]
    public void DistanciaKm_InvertendoOrigemEDestino_RetornaMesmoValor()
    {
        // Arrange
        var ida = GeoCalculator.DistanciaKm(-30.0346, -51.2177, -29.9178, -51.1839);

        // Act
        var volta = GeoCalculator.DistanciaKm(-29.9178, -51.1839, -30.0346, -51.2177);

        // Assert
        Assert.Equal(ida, volta, 9);
    }
}

public class RiscoCalculatorTests
{
    [Theory]
    [InlineData(0, NivelRisco.Baixo)]
    [InlineData(1, NivelRisco.Baixo)]
    [InlineData(2, NivelRisco.Baixo)]
    [InlineData(3, NivelRisco.Medio)]
    [InlineData(4, NivelRisco.Alto)]
    [InlineData(5, NivelRisco.Critico)]
    public void Classificar_ConformeMaiorGravidade_RetornaNivelDeRisco(int gravidade, NivelRisco esperado)
    {
        // Arrange & Act
        var risco = RiscoCalculator.Classificar(gravidade);

        // Assert
        Assert.Equal(esperado, risco);
    }
}

public class PagedResultTests
{
    [Fact]
    public void Construtor_PrimeiraPaginaDeTres_CalculaMetadados()
    {
        // Arrange & Act
        var pagina = new PagedResult<int>(new[] { 1, 2 }, page: 1, pageSize: 2, totalItems: 5);

        // Assert
        Assert.Equal(3, pagina.TotalPages);
        Assert.True(pagina.HasNext);
        Assert.False(pagina.HasPrevious);
    }

    [Fact]
    public void Construtor_UltimaPagina_NaoTemProxima()
    {
        // Arrange & Act
        var pagina = new PagedResult<int>(new[] { 5 }, page: 3, pageSize: 2, totalItems: 5);

        // Assert
        Assert.False(pagina.HasNext);
        Assert.True(pagina.HasPrevious);
    }

    [Fact]
    public void Map_ComConversao_PreservaMetadados()
    {
        // Arrange
        var pagina = new PagedResult<int>(new[] { 1, 2 }, 2, 2, 6);

        // Act
        var convertida = pagina.Map(x => x.ToString());

        // Assert
        Assert.Equal(new[] { "1", "2" }, convertida.Items);
        Assert.Equal(2, convertida.Page);
        Assert.Equal(6, convertida.TotalItems);
    }

    [Fact]
    public void Construtor_SemItens_TotalDePaginasZero()
    {
        // Arrange & Act
        var pagina = new PagedResult<int>(Array.Empty<int>(), 1, 10, 0);

        // Assert
        Assert.Equal(0, pagina.TotalPages);
        Assert.False(pagina.HasNext);
    }
}

public class UsuarioContextoTests
{
    [Fact]
    public void PodeVerOrgao_Admin_VeTodosOsOrgaos()
    {
        // Arrange
        var admin = TestData.Admin();

        // Act
        var pode = admin.PodeVerOrgao(OrgaoResponsavel.Policia);

        // Assert
        Assert.True(pode);
    }

    [Fact]
    public void PodeVerOrgao_GestorDaDefesaCivil_VeTodosOsOrgaos()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.DefesaCivil);

        // Act
        var pode = gestor.PodeVerOrgao(OrgaoResponsavel.Policia);

        // Assert
        Assert.True(pode);
    }

    [Fact]
    public void PodeVerOrgao_GestorDeOutroOrgao_VeSomenteOProprio()
    {
        // Arrange
        var gestor = TestData.Gestor(OrgaoResponsavel.Prefeitura);

        // Act & Assert
        Assert.True(gestor.PodeVerOrgao(OrgaoResponsavel.Prefeitura));
        Assert.False(gestor.PodeVerOrgao(OrgaoResponsavel.Policia));
        Assert.Equal(OrgaoResponsavel.Prefeitura, gestor.OrgaoRestrito);
    }

    [Fact]
    public void PodeVerOrgao_Cidadao_NaoVeNenhumOrgao()
    {
        // Arrange
        var cidadao = TestData.Cidadao();

        // Act
        var pode = cidadao.PodeVerOrgao(OrgaoResponsavel.Prefeitura);

        // Assert
        Assert.False(pode);
    }

    [Fact]
    public void OrgaoRestrito_AdminEDefesaCivil_NaoTemRestricao()
    {
        // Arrange & Act & Assert
        Assert.Null(TestData.Admin().OrgaoRestrito);
        Assert.Null(TestData.Gestor(OrgaoResponsavel.DefesaCivil).OrgaoRestrito);
    }
}

public class SolicitacaoAcessoTests
{
    [Fact]
    public void Pode_CidadaoAutor_RetornaVerdadeiro()
    {
        // Arrange
        var cidadao = TestData.Cidadao();
        var solicitacao = TestData.Solicitacao(cidadao.Id, Guid.NewGuid());

        // Act
        var pode = SolicitacaoAcesso.Pode(cidadao, solicitacao);

        // Assert
        Assert.True(pode);
    }

    [Fact]
    public void Pode_OutroCidadao_RetornaFalso()
    {
        // Arrange
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var pode = SolicitacaoAcesso.Pode(TestData.Cidadao(), solicitacao);

        // Assert
        Assert.False(pode);
    }

    [Fact]
    public void Pode_GestorDoOrgaoDeDestino_RetornaVerdadeiro()
    {
        // Arrange
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid(), TipoSolicitacao.AcessoNegado);

        // Act
        var pode = SolicitacaoAcesso.Pode(TestData.Gestor(OrgaoResponsavel.Policia), solicitacao);

        // Assert
        Assert.True(pode);
    }

    [Fact]
    public void Pode_GestorDeOutroOrgao_RetornaFalso()
    {
        // Arrange
        var solicitacao = TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid(), TipoSolicitacao.AcessoNegado);

        // Act
        var pode = SolicitacaoAcesso.Pode(TestData.Gestor(OrgaoResponsavel.Prefeitura), solicitacao);

        // Assert
        Assert.False(pode);
    }
}
