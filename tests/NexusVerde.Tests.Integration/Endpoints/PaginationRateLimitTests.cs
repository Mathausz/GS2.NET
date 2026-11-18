using System.Net;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class PaginationFilterSortTests(NexusVerdeFactory factory)
{
    /// <summary>Cria 3 regiões com um prefixo único (o banco é compartilhado entre os testes da coleção).</summary>
    private async Task<(HttpClient Admin, string Token)> AdminComTresRegioesAsync()
    {
        var admin = await factory.ComoAdminAsync();
        var token = Guid.NewGuid().ToString("N");
        await admin.CriarRegiaoAsync($"{token}-Alfa", "RS");
        await admin.CriarRegiaoAsync($"{token}-Bravo", "RS");
        await admin.CriarRegiaoAsync($"{token}-Charlie", "SC");
        return (admin, token);
    }

    [Fact]
    public async Task GetRegioes_PrimeiraPaginaDeDois_RetornaMetadadosCorretos()
    {
        // Arrange
        var (admin, token) = await AdminComTresRegioesAsync();

        // Act
        var json = await (await admin.GetAsync($"/api/v1/regioes?busca={token}&page=1&pageSize=2")).LerJsonAsync();

        // Assert
        Assert.Equal(2, json["items"]!.AsArray().Count);
        Assert.Equal(3, json["totalItems"]!.GetValue<int>());
        Assert.Equal(2, json["totalPages"]!.GetValue<int>());
        Assert.True(json["hasNext"]!.GetValue<bool>());
        Assert.False(json["hasPrevious"]!.GetValue<bool>());
    }

    [Fact]
    public async Task GetRegioes_OrdenadoPorNomeDecrescente_RetornaCharlieNoTopo()
    {
        // Arrange
        var (admin, token) = await AdminComTresRegioesAsync();

        // Act
        var json = await (await admin.GetAsync($"/api/v1/regioes?busca={token}&sortBy=nome&desc=true")).LerJsonAsync();

        // Assert
        var nomes = json["items"]!.AsArray().Select(i => i!["nome"]!.GetValue<string>()).ToList();
        Assert.Equal($"{token}-Charlie", nomes.First());
        Assert.Equal(nomes.OrderByDescending(n => n).ToList(), nomes);
    }

    [Fact]
    public async Task GetRegioes_FiltroPorEstado_RetornaSomenteDoEstado()
    {
        // Arrange
        var (admin, token) = await AdminComTresRegioesAsync();

        // Act
        var json = await (await admin.GetAsync($"/api/v1/regioes?busca={token}&estado=SC")).LerJsonAsync();

        // Assert
        Assert.Equal(1, json["totalItems"]!.GetValue<int>());
        Assert.Equal($"{token}-Charlie", json["items"]![0]!["nome"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetRegioes_FiltroPorBuscaTextual_RetornaApenasCorrespondentes()
    {
        // Arrange
        var (admin, token) = await AdminComTresRegioesAsync();

        // Act
        var json = await (await admin.GetAsync($"/api/v1/regioes?busca={token}-brav")).LerJsonAsync();

        // Assert
        Assert.Equal(1, json["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task GetRegioes_PageSizeAcimaDoLimite_Retorna400()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync("/api/v1/regioes?pageSize=1000");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_CampoDeOrdenacaoDesconhecido_EhIgnorado()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync("/api/v1/regioes?sortBy=campoQueNaoExiste");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[Collection(RateLimitTestCollection.Name)]
public class RateLimitEndpointTests(RateLimitedFactory factory)
{
    [Fact]
    public async Task PostLogin_AcimaDoLimite_Retorna429ComRetryAfter()
    {
        // Arrange
        var client = factory.CreateClient();
        var corpo = new { email = "ninguem@teste.com", senha = "qualquer-coisa" };
        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? ultima = null;

        // Act
        for (var i = 0; i < 5; i++)
        {
            ultima = await client.PostAsJsonAsync("/api/v1/auth/login", corpo);
            statuses.Add(ultima.StatusCode);
        }

        // Assert
        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
        Assert.True(ultima!.Headers.Contains("Retry-After"));
        var problem = await ultima.LerJsonAsync();
        Assert.Equal(429, problem["status"]!.GetValue<int>());
    }
}
