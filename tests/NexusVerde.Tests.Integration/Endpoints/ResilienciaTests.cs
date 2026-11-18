using System.Net;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>Rate limiting da política "criacao" (POST /solicitacoes): acima do limite → 429 + Retry-After.</summary>
[Collection(CriacaoRateLimitTestCollection.Name)]
public class CriacaoRateLimitTests(CriacaoLimitadaFactory factory)
{
    [Fact]
    public async Task PostSolicitacoes_AcimaDoLimiteDeCriacao_Retorna429ComRetryAfterEProblemDetails()
    {
        // Arrange (limite da política "criacao" nesta factory: 2 por minuto, por usuário)
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? ultima = null;

        // Act
        for (var i = 0; i < 3; i++)
        {
            ultima = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
                ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId), Guid.NewGuid().ToString());
            statuses.Add(ultima.StatusCode);
        }

        // Assert
        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Created, HttpStatusCode.TooManyRequests }, statuses);
        Assert.True(ultima!.Headers.Contains("Retry-After"));
        Assert.Equal("application/problem+json", ultima.Content.Headers.ContentType?.MediaType);
        var problem = await ultima.LerJsonAsync();
        Assert.Equal(429, problem["status"]!.GetValue<int>());
    }

    [Fact]
    public async Task PostSolicitacoes_LimiteDeOutroUsuarioEsgotado_NaoAfetaEsteUsuario()
    {
        // Arrange: o limite é por usuário; um cidadão esgota o dele.
        var cenario = await factory.PrepararCriseAsync();
        var primeiro = await factory.ComoCidadaoAsync();
        for (var i = 0; i < 3; i++)
            await primeiro.PostIdempotenteAsync("/api/v1/solicitacoes",
                ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId), Guid.NewGuid().ToString());
        var segundo = await factory.ComoCidadaoAsync();

        // Act
        var response = await segundo.PostIdempotenteAsync("/api/v1/solicitacoes",
            ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId), Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

/// <summary>Serviço externo (Open-Meteo) fora do ar: health check Degraded e a API continua atendendo.</summary>
[Collection(ClimaIndisponivelTestCollection.Name)]
public class ServicoExternoIndisponivelTests(ClimaIndisponivelFactory factory)
{
    [Fact]
    public async Task GetHealth_OpenMeteoIndisponivel_RetornaDegradedComBancoHealthy()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert (Degraded continua respondendo 200: a API funciona, só sem o dado de chuva)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal("Degraded", json["status"]!.GetValue<string>());
        var checks = json["checks"]!.AsArray().ToDictionary(
            c => c!["name"]!.GetValue<string>(), c => c!["status"]!.GetValue<string>());
        Assert.Equal("Degraded", checks["open-meteo"]);
        Assert.Equal("Healthy", checks["database"]);
    }

    [Fact]
    public async Task GetHealthLive_OpenMeteoIndisponivel_ContinuaHealthy()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var json = await (await client.GetAsync("/health/live")).LerJsonAsync();

        // Assert
        Assert.Equal("Healthy", json["status"]!.GetValue<string>());
    }
}
