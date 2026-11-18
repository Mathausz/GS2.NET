using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Tests.Unit.Domain;

public class UsuarioTests
{
    [Fact]
    public void Construtor_GestorSemOrgao_LancaDomainException()
    {
        // Arrange
        var perfil = PerfilUsuario.Gestor;

        // Act
        Action act = () => new Usuario("Ana", "ana@x.com", "hash", perfil);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Construtor_CidadaoComOrgaoInformado_IgnoraOrgaoENormalizaEmail()
    {
        // Arrange & Act
        var usuario = new Usuario("Ana", "ANA@X.COM", "hash", PerfilUsuario.Cidadao, OrgaoResponsavel.Policia);

        // Assert
        Assert.Equal(OrgaoResponsavel.Nenhum, usuario.Orgao);
        Assert.Equal("ana@x.com", usuario.Email);
    }

    [Fact]
    public void Construtor_GestorComOrgao_MantemOrgao()
    {
        // Arrange & Act
        var usuario = new Usuario("Rui", "rui@x.com", "hash", PerfilUsuario.Gestor, OrgaoResponsavel.Bombeiros);

        // Assert
        Assert.Equal(OrgaoResponsavel.Bombeiros, usuario.Orgao);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_NomeVazio_LancaDomainException(string nome)
    {
        // Arrange & Act
        Action act = () => new Usuario(nome, "ana@x.com", "hash", PerfilUsuario.Cidadao);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Desativar_UsuarioAtivo_MarcaComoInativo()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@x.com", "hash", PerfilUsuario.Cidadao);

        // Act
        usuario.Desativar();

        // Assert
        Assert.False(usuario.Ativo);
        Assert.NotNull(usuario.AtualizadoEm);
    }
}

public class RegiaoTests
{
    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Construtor_CoordenadasInvalidas_LancaDomainException(double lat, double lng)
    {
        // Arrange & Act
        Action act = () => new Regiao("Centro", "POA", "RS", lat, lng, 10);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Construtor_EstadoEmMinusculas_NormalizaParaMaiusculas()
    {
        // Arrange & Act
        var regiao = new Regiao("Centro", "POA", "rs", -30, -51, 10);

        // Assert
        Assert.Equal("RS", regiao.Estado);
        Assert.Equal(NivelRisco.Baixo, regiao.NivelRisco);
    }

    [Fact]
    public void Construtor_PopulacaoNegativa_LancaDomainException()
    {
        // Arrange & Act
        Action act = () => new Regiao("Centro", "POA", "RS", -30, -51, -1);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void DefinirRisco_NovoNivel_AtualizaRegiao()
    {
        // Arrange
        var regiao = new Regiao("Centro", "POA", "RS", -30, -51, 10);

        // Act
        regiao.DefinirRisco(NivelRisco.Critico);

        // Assert
        Assert.Equal(NivelRisco.Critico, regiao.NivelRisco);
    }

    [Fact]
    public void Atualizar_DadosValidos_SubstituiCampos()
    {
        // Arrange
        var regiao = new Regiao("Centro", "POA", "RS", -30, -51, 10);

        // Act
        regiao.Atualizar("Norte", "Canoas", "rs", -29.9, -51.1, 500);

        // Assert
        Assert.Equal("Norte", regiao.Nome);
        Assert.Equal("Canoas", regiao.Municipio);
        Assert.Equal(500, regiao.PopulacaoEstimada);
    }
}

public class DesastreTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Construtor_GravidadeForaDaFaixa_LancaDomainException(int gravidade)
    {
        // Arrange & Act
        Action act = () => new Desastre(Guid.NewGuid(), TipoDesastre.Enchente, gravidade, "Alagamento");

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Atualizar_StatusEncerrado_RegistraDataEDesativa()
    {
        // Arrange
        var desastre = new Desastre(Guid.NewGuid(), TipoDesastre.Enchente, 3, "Alagamento");

        // Act
        desastre.Atualizar(TipoDesastre.Enchente, 3, "Alagamento", StatusDesastre.Encerrado);

        // Assert
        Assert.False(desastre.EstaAtivo);
        Assert.NotNull(desastre.EncerradoEm);
    }

    [Fact]
    public void Atualizar_ReabrirDesastreEncerrado_LancaDomainException()
    {
        // Arrange
        var desastre = new Desastre(Guid.NewGuid(), TipoDesastre.Seca, 2, "Seca prolongada");
        desastre.Atualizar(TipoDesastre.Seca, 2, "Seca prolongada", StatusDesastre.Encerrado);

        // Act
        Action act = () => desastre.Atualizar(TipoDesastre.Seca, 2, "Seca prolongada", StatusDesastre.Ativo);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Atualizar_StatusControlado_ContinuaAtivo()
    {
        // Arrange
        var desastre = new Desastre(Guid.NewGuid(), TipoDesastre.Enchente, 4, "Alagamento");

        // Act
        desastre.Atualizar(TipoDesastre.Enchente, 3, "Alagamento controlado", StatusDesastre.Controlado);

        // Assert
        Assert.True(desastre.EstaAtivo);
        Assert.Null(desastre.EncerradoEm);
        Assert.Equal(3, desastre.Gravidade);
    }
}
