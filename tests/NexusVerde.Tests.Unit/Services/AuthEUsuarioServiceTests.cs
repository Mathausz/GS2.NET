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

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenGenerator> _tokens = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-gerado");
        _service = new AuthService(_usuarios.Object, _uow.Object, _hasher.Object, _tokens.Object);
    }

    [Fact]
    public async Task RegistrarAsync_EmailNovo_CriaCidadaoComEmailNormalizado()
    {
        // Arrange
        Usuario? salvo = null;
        _usuarios.Setup(r => r.GetByEmailAsync("ana@x.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _usuarios.Setup(r => r.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, _) => salvo = u);

        // Act
        var resposta = await _service.RegistrarAsync(
            new RegistroRequest { Nome = "Ana", Email = "  ANA@x.com ", Senha = "senha-forte-1" });

        // Assert
        Assert.NotNull(salvo);
        Assert.Equal(PerfilUsuario.Cidadao, salvo!.Perfil);
        Assert.Equal("hash-gerado", salvo.SenhaHash);
        Assert.Equal("ana@x.com", resposta.Email);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_EmailExistente_LancaConflictException()
    {
        // Arrange
        _usuarios.Setup(r => r.GetByEmailAsync("ana@x.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao));

        // Act
        Func<Task> act = () => _service.RegistrarAsync(
            new RegistroRequest { Nome = "Ana", Email = "ana@x.com", Senha = "senha-forte-1" });

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_LancaUnauthorizedException()
    {
        // Arrange
        _usuarios.Setup(r => r.GetByEmailAsync("ana@x.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao));
        _hasher.Setup(h => h.Verify("errada", "h")).Returns(false);

        // Act
        Func<Task> act = () => _service.LoginAsync(new LoginRequest { Email = "ana@x.com", Senha = "errada" });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
    }

    [Fact]
    public async Task LoginAsync_UsuarioInexistente_LancaUnauthorizedException()
    {
        // Arrange
        _usuarios.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        // Act
        Func<Task> act = () => _service.LoginAsync(new LoginRequest { Email = "x@x.com", Senha = "qualquer" });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
    }

    [Fact]
    public async Task LoginAsync_UsuarioDesativado_LancaUnauthorizedException()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao);
        usuario.Desativar();
        _usuarios.Setup(r => r.GetByEmailAsync("ana@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Act
        Func<Task> act = () => _service.LoginAsync(new LoginRequest { Email = "ana@x.com", Senha = "senha" });

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(act);
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaTokenEUsuario()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao);
        var expira = DateTime.UtcNow.AddHours(1);
        _usuarios.Setup(r => r.GetByEmailAsync("ana@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _hasher.Setup(h => h.Verify("senha", "h")).Returns(true);
        _tokens.Setup(t => t.Generate(usuario)).Returns(("jwt-token", expira));

        // Act
        var resposta = await _service.LoginAsync(new LoginRequest { Email = "ANA@x.com", Senha = "senha" });

        // Assert
        Assert.Equal("jwt-token", resposta.Token);
        Assert.Equal(expira, resposta.ExpiraEm);
        Assert.Equal(usuario.Id, resposta.Usuario.Id);
    }
}

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash-gerado");
        _service = new UsuarioService(_usuarios.Object, _uow.Object, _hasher.Object);
    }

    [Fact]
    public async Task CriarAsync_GestorComOrgao_PersisteUsuario()
    {
        // Arrange
        Usuario? salvo = null;
        _usuarios.Setup(r => r.GetByEmailAsync("rui@x.com", It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        _usuarios.Setup(r => r.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, _) => salvo = u);

        // Act
        var resposta = await _service.CriarAsync(new UsuarioCreateRequest
        {
            Nome = "Rui", Email = "RUI@x.com", Senha = "senha-forte-1", Perfil = PerfilUsuario.Gestor, Orgao = OrgaoResponsavel.Bombeiros
        });

        // Assert
        Assert.Equal(OrgaoResponsavel.Bombeiros, salvo!.Orgao);
        Assert.Equal("rui@x.com", resposta.Email);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_GestorSemOrgao_LancaDomainException()
    {
        // Arrange
        _usuarios.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        // Act
        Func<Task> act = () => _service.CriarAsync(new UsuarioCreateRequest
        {
            Nome = "Rui", Email = "rui@x.com", Senha = "senha-forte-1", Perfil = PerfilUsuario.Gestor, Orgao = OrgaoResponsavel.Nenhum
        });

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task CriarAsync_EmailExistente_LancaConflictException()
    {
        // Arrange
        _usuarios.Setup(r => r.GetByEmailAsync("rui@x.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario("Rui", "rui@x.com", "h", PerfilUsuario.Cidadao));

        // Act
        Func<Task> act = () => _service.CriarAsync(new UsuarioCreateRequest
        {
            Nome = "Rui", Email = "rui@x.com", Senha = "senha-forte-1", Perfil = PerfilUsuario.Cidadao
        });

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
    }

    [Fact]
    public async Task ObterAsync_UsuarioInexistente_LancaNotFoundException()
    {
        // Arrange
        _usuarios.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        // Act
        Func<Task> act = () => _service.ObterAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }

    [Fact]
    public async Task ObterAsync_UsuarioExistente_RetornaResposta()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao);
        _usuarios.Setup(r => r.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        // Act
        var resposta = await _service.ObterAsync(usuario.Id);

        // Assert
        Assert.Equal("ana@x.com", resposta.Email);
    }

    [Fact]
    public async Task ListarAsync_RepositorioComUsuarios_MapeiaPagina()
    {
        // Arrange
        var query = new UsuarioQuery();
        var usuario = new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao);
        _usuarios.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Usuario>(new List<Usuario> { usuario }, 1, 10, 1));

        // Act
        var pagina = await _service.ListarAsync(query);

        // Assert
        Assert.Single(pagina.Items);
        Assert.Equal(1, pagina.TotalItems);
    }

    [Fact]
    public async Task DesativarAsync_ProprioUsuario_LancaConflictException()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        Func<Task> act = () => _service.DesativarAsync(id, id);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(act);
    }

    [Fact]
    public async Task DesativarAsync_OutroUsuario_MarcaComoInativo()
    {
        // Arrange
        var usuario = new Usuario("Ana", "ana@x.com", "h", PerfilUsuario.Cidadao);
        _usuarios.Setup(r => r.GetByIdAsync(usuario.Id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        // Act
        await _service.DesativarAsync(usuario.Id, Guid.NewGuid());

        // Assert
        Assert.False(usuario.Ativo);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
