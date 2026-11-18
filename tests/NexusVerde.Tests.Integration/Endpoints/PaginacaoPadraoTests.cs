using System.Net;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>
/// Paginação opcional: as listagens precisam funcionar sem nenhum parâmetro de query (como o app mobile chama),
/// aplicando página 1 e 10 itens por padrão.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PaginacaoPadraoTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task GetRegioes_SemParametrosDePaginacao_Retorna200ComValoresPadrao()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        await admin.CriarRegiaoAsync($"Região {Guid.NewGuid():N}");

        // Act
        var response = await admin.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal(1, json["page"]!.GetValue<int>());
        Assert.Equal(10, json["pageSize"]!.GetValue<int>());
        Assert.True(json["totalItems"]!.GetValue<int>() >= 1);
    }

    [Fact]
    public async Task GetSolicitacoesMinhas_SemParametros_Retorna200ComAsSolicitacoesDoCidadao()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await cidadao.GetAsync("/api/v1/solicitacoes/minhas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal(1, json["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task GetRegioes_SomenteOrdenacaoDecrescente_Retorna200()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync("/api/v1/regioes?desc=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_PaginaZero_Retorna400()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync("/api/v1/regioes?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerJson_ListagemDeRegioes_DocumentaPaginacaoOpcionalEmCamelCase()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var json = await (await client.GetAsync("/swagger/v1/swagger.json")).LerJsonAsync();

        // Assert
        var parametros = json["paths"]!["/api/v1/regioes"]!["get"]!["parameters"]!.AsArray();
        var nomes = parametros.Select(p => p!["name"]!.GetValue<string>()).ToList();
        Assert.Contains("page", nomes);
        Assert.Contains("pageSize", nomes);
        Assert.Contains("desc", nomes);
        Assert.All(parametros, p => Assert.False(p!["required"]?.GetValue<bool>() ?? false));
    }
}
