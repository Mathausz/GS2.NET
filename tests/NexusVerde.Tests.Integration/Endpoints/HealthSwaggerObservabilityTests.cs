using System.Net;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class HealthEndpointTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task GetHealth_TodasAsDependencias_RetornaHealthyComOsTresChecks()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal("Healthy", json["status"]!.GetValue<string>());
        var nomes = json["checks"]!.AsArray().Select(c => c!["name"]!.GetValue<string>()).ToList();
        Assert.Contains("api", nomes);
        Assert.Contains("database", nomes);
        Assert.Contains("open-meteo", nomes);
        Assert.Equal(3, nomes.Count);
    }

    [Fact]
    public async Task GetHealthLive_SemAutenticacao_Retorna200()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthReady_DependenciasExternas_RetornaBancoEServicoExterno()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        var nomes = json["checks"]!.AsArray().Select(c => c!["name"]!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "database", "open-meteo" }, nomes.OrderBy(n => n).ToArray());
    }
}

[Collection(IntegrationTestCollection.Name)]
public class SwaggerEndpointTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task GetSwaggerJson_Documentacao_ContemEndpointsSegurancaEIdempotencia()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var documento = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/solicitacoes", documento);
        Assert.Contains("/api/v1/auth/login", documento);
        Assert.Contains("/timeline", documento);
        Assert.Contains("Idempotency-Key", documento);
        Assert.Contains("Bearer", documento);
    }
}

[Collection(IntegrationTestCollection.Name)]
public class ObservabilityEndpointTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task GetQualquerEndpoint_SemCorrelationId_GeraEDevolveOHeader()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var valores));
        Assert.False(string.IsNullOrWhiteSpace(valores!.First()));
    }

    [Fact]
    public async Task GetQualquerEndpoint_ComCorrelationIdInformado_PropagaOMesmoValor()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", "teste-correlacao-123");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal("teste-correlacao-123", response.Headers.GetValues("X-Correlation-Id").First());
    }

    [Fact]
    public async Task GetMetrics_AposRequisicoes_ExpoeTempoDeRespostaETaxaDeErro()
    {
        // Arrange
        var client = factory.CreateClient();
        await client.GetAsync("/api/v1/regioes"); // 401: alimenta a taxa de erros

        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.Content.Headers.ContentType!.ToString());
        var texto = await response.Content.ReadAsStringAsync();
        Assert.Contains("nexusverde_http_requests_total", texto);
        Assert.Contains("nexusverde_http_request_duration_ms_bucket", texto);
        Assert.Contains("nexusverde_http_error_ratio", texto);
        Assert.Contains("status=\"401\"", texto);
    }

    [Fact]
    public async Task GetMetrics_AposCriarSolicitacao_ExpoeMetricaDeNegocio()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var texto = await factory.CreateClient().GetStringAsync("/metrics");

        // Assert
        Assert.Contains("nexusverde_solicitacoes_criadas_total", texto);
        Assert.Contains("tipo=\"SemAgua\"", texto);
    }

    [Fact]
    public async Task GetMetrics_RepeticaoIdempotente_ContabilizaReplay()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var chave = Guid.NewGuid().ToString();
        var pedido = ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId);
        await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);
        await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);

        // Act
        var texto = await factory.CreateClient().GetStringAsync("/metrics");

        // Assert
        Assert.Contains("nexusverde_idempotency_replays_total", texto);
    }

    [Fact]
    public async Task GetRecursoInexistente_ErroDeNegocio_RetornaProblemDetailsComCorrelationId()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync($"/api/v1/regioes/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal(404, json["status"]!.GetValue<int>());
        Assert.NotNull(json["correlationId"]);
    }
}
