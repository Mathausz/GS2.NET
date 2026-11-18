using System.Net;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>Linha do tempo da solicitação (tabela NV_TIMELINE; EF InMemory nos testes) e notas dos gestores.</summary>
[Collection(IntegrationTestCollection.Name)]
public class TimelineEndpointTests(NexusVerdeFactory factory)
{
    private static List<string> Tipos(System.Text.Json.Nodes.JsonNode pagina) =>
        pagina["items"]!.AsArray().Select(i => i!["tipo"]!.GetValue<string>()).ToList();

    [Fact]
    public async Task GetTimeline_AposCriarSolicitacao_RetornaEventoCriada()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal(new[] { "Criada" }, Tipos(json));
        Assert.NotNull(json["items"]![0]!["dados"]!["score"]);
    }

    [Fact]
    public async Task GetTimeline_FluxoCompleto_RetornaEventosEmOrdemCronologica()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        var atendimento = await gestor.PostIdempotenteAsync("/api/v1/atendimentos",
            new { solicitacaoId = id }, Guid.NewGuid().ToString());
        var atendimentoId = (await atendimento.LerJsonAsync())["id"]!.GetValue<string>();
        await gestor.PatchAsJsonAsync($"/api/v1/atendimentos/{atendimentoId}/status", new { status = "Concluido" });

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline")).LerJsonAsync();

        // Assert
        Assert.Equal(new[] { "Criada", "AtendimentoIniciado", "AtendimentoConcluido" }, Tipos(json));
    }

    [Fact]
    public async Task GetTimeline_OrdemDecrescente_InverteAOrdem()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas", new { texto = "Equipe a caminho." }, Guid.NewGuid().ToString());

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline?desc=true")).LerJsonAsync();

        // Assert
        Assert.Equal(new[] { "NotaAdicionada", "Criada" }, Tipos(json));
    }

    [Fact]
    public async Task GetTimeline_FiltroPorTipo_RetornaSomenteEventosDoTipo()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas", new { texto = "Primeira nota." }, Guid.NewGuid().ToString());

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline?tipo=NotaAdicionada")).LerJsonAsync();

        // Assert
        Assert.Equal(new[] { "NotaAdicionada" }, Tipos(json));
    }

    [Fact]
    public async Task GetTimeline_PaginaComUmItem_RetornaMetadadosDePaginacao()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas", new { texto = "Segundo evento." }, Guid.NewGuid().ToString());

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline?page=1&pageSize=1")).LerJsonAsync();

        // Assert
        Assert.Equal(1, json["items"]!.AsArray().Count);
        Assert.Equal(2, json["totalItems"]!.GetValue<int>());
        Assert.Equal(2, json["totalPages"]!.GetValue<int>());
        Assert.Contains("next", json.Rels());
    }

    [Fact]
    public async Task GetTimeline_OutroCidadao_Retorna403()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var autor = await factory.ComoCidadaoAsync();
        var outro = await factory.ComoCidadaoAsync();
        var id = await autor.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await outro.GetAsync($"/api/v1/solicitacoes/{id}/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_GestorDeOutroOrgao_Retorna403()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var policia = await factory.ComoGestorAsync(cenario.Admin, "Policia");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId, "SemAgua");

        // Act
        var response = await policia.GetAsync($"/api/v1/solicitacoes/{id}/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_SolicitacaoInexistente_Retorna404()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();

        // Act
        var response = await admin.GetAsync($"/api/v1/solicitacoes/{Guid.NewGuid()}/timeline");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostNotas_GestorDoOrgao_Retorna201EApareceNaTimeline()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas",
            new { texto = "Caminhão-pipa chega em 30 minutos." }, Guid.NewGuid().ToString());
        var timeline = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline?tipo=NotaAdicionada")).LerJsonAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Caminhão-pipa chega em 30 minutos.", timeline["items"]![0]!["descricao"]!.GetValue<string>());
        Assert.Equal("Gestor", timeline["items"]![0]!["autorPerfil"]!.GetValue<string>());
    }

    [Fact]
    public async Task PostNotas_PerfilCidadao_Retorna403()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await cidadao.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas",
            new { texto = "Tentando anotar." }, Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostNotas_SemIdempotencyKey_Retorna400()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas",
            new { texto = "Sem chave de idempotência." }, chave: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostNotas_MesmaChaveRepetida_NaoDuplicaANota()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        var chave = Guid.NewGuid().ToString();
        var corpo = new { texto = "Nota enviada duas vezes." };

        // Act
        await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas", corpo, chave);
        var repetida = await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas", corpo, chave);
        var timeline = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline?tipo=NotaAdicionada")).LerJsonAsync();

        // Assert
        Assert.True(repetida.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(1, timeline["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task PostNotas_TextoMuitoCurto_Retorna400()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas",
            new { texto = "oi" }, Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostNotas_SolicitacaoCancelada_Retorna422()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await cidadao.PatchAsJsonAsync($"/api/v1/solicitacoes/{id}/cancelar", new { });

        // Act
        var response = await gestor.PostIdempotenteAsync($"/api/v1/solicitacoes/{id}/notas",
            new { texto = "Nota em solicitação cancelada." }, Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_AposCancelamentoDoCidadao_RegistraEventoCancelada()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await cidadao.PatchAsJsonAsync($"/api/v1/solicitacoes/{id}/cancelar", new { });

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline")).LerJsonAsync();

        // Assert
        Assert.Equal(new[] { "Criada", "Cancelada" }, Tipos(json));
    }
}
