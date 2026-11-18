using System.ComponentModel.DataAnnotations;
using NexusVerde.Application.Common;
using NexusVerde.Application.Queries;

namespace NexusVerde.Tests.Unit.Application;

/// <summary>Paginação opcional: valores padrão, limites e validação dos parâmetros de listagem.</summary>
public class QueryParametersTests
{
    private static List<ValidationResult> Validar(object query)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), resultados, validateAllProperties: true);
        return resultados;
    }

    [Fact]
    public void ValoresEfetivos_SemNenhumParametro_UsamPaginaUmDezItensOrdemCrescente()
    {
        // Arrange
        var query = new RegiaoQuery();

        // Act & Assert
        Assert.Equal(1, query.PaginaEfetiva());
        Assert.Equal(QueryParameters.PageSizePadrao, query.TamanhoEfetivo());
        Assert.False(query.Decrescente());
    }

    [Fact]
    public void ValoresEfetivos_ParametrosInformados_SaoRespeitados()
    {
        // Arrange
        var query = new RegiaoQuery { Page = 3, PageSize = 25, Desc = true };

        // Act & Assert
        Assert.Equal(3, query.PaginaEfetiva());
        Assert.Equal(25, query.TamanhoEfetivo());
        Assert.True(query.Decrescente());
    }

    [Fact]
    public void TamanhoEfetivo_AcimaDoMaximo_LimitaEm50()
    {
        // Arrange
        var query = new SolicitacaoQuery { PageSize = 500 };

        // Act & Assert
        Assert.Equal(QueryParameters.PageSizeMaximo, query.TamanhoEfetivo());
    }

    [Fact]
    public void Validacao_SemParametros_EhValida()
    {
        // Act
        var erros = Validar(new OrientacaoQuery());

        // Assert
        Assert.Empty(erros);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, 0)]
    [InlineData(null, 51)]
    public void Validacao_ValoresForaDoIntervalo_RetornaErro(int? page, int? pageSize)
    {
        // Act
        var erros = Validar(new UsuarioQuery { Page = page, PageSize = pageSize });

        // Assert
        Assert.NotEmpty(erros);
    }
}
