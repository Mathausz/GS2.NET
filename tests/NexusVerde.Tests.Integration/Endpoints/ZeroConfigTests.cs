using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>
/// "dotnet run" sem nenhuma configuração (Development): a API precisa subir com banco em memória,
/// chave JWT temporária, dados de demonstração e os usuários de demo do appsettings.Development.json.
/// </summary>
[Collection(ZeroConfigTestCollection.Name)]
public class ZeroConfigTests(ZeroConfigFactory factory)
{
    private async Task<HttpClient> LoginAsync(string email, string senha)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.LerJsonAsync())["token"]!.GetValue<string>();

        var autenticado = factory.CreateClient();
        autenticado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return autenticado;
    }

    [Fact]
    public async Task GetHealth_SemConfiguracao_ApiSobeComBancoEmMemoriaHealthy()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal("Healthy", json["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetSwagger_SemConfiguracao_DocumentacaoDisponivel()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_AdminDeDemonstracao_ListaRegioesDoSeed()
    {
        // Arrange
        var admin = await LoginAsync("admin@nexusverde.demo", "Admin#Demo2026");

        // Act
        var response = await admin.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.True(json["totalItems"]!.GetValue<int>() >= 3);
    }

    [Fact]
    public async Task FluxoDemo_CidadaoPedeAjudaEGestorDaDefesaCivilVeNaFila()
    {
        // Arrange
        var cidadao = await LoginAsync("cidadao@nexusverde.demo", "Cidadao#Demo2026");
        var gestor = await LoginAsync("gestor@nexusverde.demo", "Gestor#Demo2026");
        var regioes = await (await cidadao.GetAsync("/api/v1/regioes")).LerJsonAsync();
        var regiaoId = regioes["items"]![0]!["id"]!.GetValue<string>();

        // Act: água suspeita é roteada para a Defesa Civil.
        var criada = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            ApiTestHelpers.PedidoDeAjuda(regiaoId, "AguaSuspeita"), Guid.NewGuid().ToString());
        var fila = await gestor.GetAsync("/api/v1/solicitacoes/fila");

        // Assert
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var id = (await criada.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.OK, fila.StatusCode);
        var itens = (await fila.LerJsonAsync())["items"]!.AsArray();
        Assert.Contains(itens, s => s!["id"]!.GetValue<string>() == id);
    }
}
