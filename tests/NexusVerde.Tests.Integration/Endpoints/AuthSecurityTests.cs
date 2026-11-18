using System.Net;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class AuthEndpointsTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task PostRegistro_DadosValidos_CriaCidadaoSemExporSenha()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"maria-{Guid.NewGuid():N}@teste.com";

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/registro",
            new { nome = "Maria", email, senha = "Senha#12345" });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal("Cidadao", json["perfil"]!.GetValue<string>());
        Assert.Null(json["senha"]);
        Assert.Null(json["senhaHash"]);
    }

    [Fact]
    public async Task PostRegistro_EmailDuplicado_Retorna409()
    {
        // Arrange
        var client = factory.CreateClient();
        var corpo = new { nome = "João", email = $"joao-{Guid.NewGuid():N}@teste.com", senha = "Senha#12345" };
        await client.PostAsJsonAsync("/api/v1/auth/registro", corpo);

        // Act
        var segunda = await client.PostAsJsonAsync("/api/v1/auth/registro", corpo);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task PostRegistro_DadosInvalidos_Retorna400ComErros()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/registro",
            new { nome = "A", email = "nao-e-email", senha = "123" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.NotNull(json["errors"]);
    }

    [Fact]
    public async Task PostLogin_CredenciaisValidas_RetornaTokenJwt()
    {
        // Arrange
        var client = factory.CreateClient();
        var email = $"ana-{Guid.NewGuid():N}@teste.com";
        await client.PostAsJsonAsync("/api/v1/auth/registro", new { nome = "Ana", email, senha = "Senha#12345" });

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "Senha#12345" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal(3, json["token"]!.GetValue<string>().Split('.').Length);
    }

    [Fact]
    public async Task PostLogin_SenhaErrada_Retorna401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = NexusVerdeFactory.AdminEmail, senha = "senha-errada" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

[Collection(IntegrationTestCollection.Name)]
public class SecurityEndpointsTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task GetRegioes_SemToken_Retorna401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_TokenInvalido_Retorna401()
    {
        // Arrange
        var client = factory.ComToken("token.invalido.qualquer");

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostRegioes_PerfilCidadao_Retorna403()
    {
        // Arrange
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.PostAsJsonAsync("/api/v1/regioes", new
        {
            nome = "X", municipio = "Y", estado = "RS", latitude = -30, longitude = -51, populacaoEstimada = 10
        });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFila_PerfilCidadao_Retorna403()
    {
        // Arrange
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.GetAsync("/api/v1/solicitacoes/fila");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_PerfilGestor_Retorna403()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        var gestor = await factory.ComoGestorAsync(admin, "Prefeitura");

        // Act
        var response = await gestor.GetAsync("/api/v1/usuarios");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostUsuarios_GestorSemOrgao_Retorna422()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/usuarios", new
        {
            nome = "Gestor", email = $"sem-orgao-{Guid.NewGuid():N}@teste.com", senha = "Gestor#12345",
            perfil = "Gestor", orgao = "Nenhum"
        });

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetOrientacoes_SemAutenticacao_Retorna200()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/orientacoes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsuarios_PerfilAdmin_RetornaListaPaginada()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync("/api/v1/usuarios?pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.True(json["totalItems"]!.GetValue<int>() >= 1);
    }
}
