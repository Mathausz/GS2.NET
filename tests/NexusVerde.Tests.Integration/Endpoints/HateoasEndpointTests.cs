using System.Net;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>HATEOAS nos endpoints de consulta: os links dependem do perfil de quem consulta e do status do recurso.</summary>
[Collection(IntegrationTestCollection.Name)]
public class HateoasEndpointTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task GetRegiao_PerfilAdmin_RetornaLinksDeLeituraEEscrita()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        var id = await admin.CriarRegiaoAsync($"Hateoas {Guid.NewGuid():N}");

        // Act
        var json = await (await admin.GetAsync($"/api/v1/regioes/{id}")).LerJsonAsync();

        // Assert
        var rels = json.Rels();
        Assert.Contains("self", rels);
        Assert.Contains("desastres", rels);
        Assert.Contains("atualizar", rels);
        Assert.Contains("remover", rels);
    }

    [Fact]
    public async Task GetRegiao_PerfilCidadao_NaoRetornaLinksDeEscrita()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        var id = await admin.CriarRegiaoAsync($"Hateoas {Guid.NewGuid():N}");
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/regioes/{id}")).LerJsonAsync();

        // Assert
        var rels = json.Rels();
        Assert.Contains("self", rels);
        Assert.DoesNotContain("atualizar", rels);
        Assert.DoesNotContain("remover", rels);
    }

    [Fact]
    public async Task GetRegioes_ListaComVariasPaginas_RetornaLinksDePaginaPreservandoFiltro()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        var token = Guid.NewGuid().ToString("N");
        await admin.CriarRegiaoAsync($"{token}-A");
        await admin.CriarRegiaoAsync($"{token}-B");
        await admin.CriarRegiaoAsync($"{token}-C");

        // Act
        var json = await (await admin.GetAsync($"/api/v1/regioes?busca={token}&page=1&pageSize=2")).LerJsonAsync();

        // Assert
        var rels = json.Rels();
        Assert.Contains("self", rels);
        Assert.Contains("next", rels);
        Assert.DoesNotContain("prev", rels);
        var next = json["_links"]!.AsArray().First(l => l!["rel"]!.GetValue<string>() == "next")!["href"]!.GetValue<string>();
        Assert.Contains(token, next);
        Assert.Contains("page=2", next);
        Assert.Contains("self", json["items"]![0]!.Rels());
    }

    [Fact]
    public async Task GetSolicitacao_AutorComSolicitacaoAberta_RetornaLinkCancelar()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}")).LerJsonAsync();

        // Assert
        var rels = json.Rels();
        Assert.Contains("cancelar", rels);
        Assert.Contains("timeline", rels);
        Assert.DoesNotContain("assumir-atendimento", rels);
    }

    [Fact]
    public async Task GetSolicitacao_GestorDoOrgaoComSolicitacaoAberta_RetornaLinkAssumirAtendimento()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var json = await (await gestor.GetAsync($"/api/v1/solicitacoes/{id}")).LerJsonAsync();

        // Assert
        var rels = json.Rels();
        Assert.Contains("assumir-atendimento", rels);
        Assert.Contains("adicionar-nota", rels);
        Assert.DoesNotContain("cancelar", rels);
    }

    [Fact]
    public async Task GetSolicitacao_AposAtendimentoConcluido_NaoRetornaAcoesDeEscrita()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        var atendimento = await gestor.PostIdempotenteAsync("/api/v1/atendimentos", new { solicitacaoId = id }, Guid.NewGuid().ToString());
        var atendimentoId = (await atendimento.LerJsonAsync())["id"]!.GetValue<string>();
        await gestor.PatchAsJsonAsync($"/api/v1/atendimentos/{atendimentoId}/status", new { status = "Concluido" });

        // Act
        var comoCidadao = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}")).LerJsonAsync();
        var comoGestor = await (await gestor.GetAsync($"/api/v1/solicitacoes/{id}")).LerJsonAsync();

        // Assert
        Assert.DoesNotContain("cancelar", comoCidadao.Rels());
        Assert.DoesNotContain("assumir-atendimento", comoGestor.Rels());
        Assert.DoesNotContain("adicionar-nota", comoGestor.Rels());
    }

    [Fact]
    public async Task GetAtendimento_GestorResponsavelComAtendimentoEmAndamento_RetornaLinksConcluirECancelar()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        var atendimento = await gestor.PostIdempotenteAsync("/api/v1/atendimentos", new { solicitacaoId = id }, Guid.NewGuid().ToString());
        var atendimentoId = (await atendimento.LerJsonAsync())["id"]!.GetValue<string>();

        // Act
        var json = await (await gestor.GetAsync($"/api/v1/atendimentos/{atendimentoId}")).LerJsonAsync();

        // Assert
        var rels = json.Rels();
        Assert.Contains("concluir", rels);
        Assert.Contains("cancelar", rels);
        Assert.Contains("solicitacao", rels);
    }

    [Fact]
    public async Task GetOrientacoes_Anonimo_RetornaLinksSomenteDeLeitura()
    {
        // Arrange
        await factory.PrepararCriseAsync();
        var anonimo = factory.CreateClient();

        // Act
        var json = await (await anonimo.GetAsync("/api/v1/orientacoes?pageSize=5")).LerJsonAsync();

        // Assert
        Assert.Contains("self", json.Rels());
        var primeiro = json["items"]![0]!.Rels();
        Assert.Contains("self", primeiro);
        Assert.DoesNotContain("atualizar", primeiro);
    }

    [Fact]
    public async Task GetPontosProximos_ComResultados_RetornaLinksNoPonto()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var (lat, lng) = ApiTestHelpers.NovaCoordenada();
        var regiaoId = await cenario.Admin.CriarRegiaoAsync($"Prox {Guid.NewGuid():N}", "RS", lat, lng);
        await cenario.Admin.CriarPontoAsync(regiaoId, "Ponto com links", lat, lng);

        // Act
        var itens = (await (await cidadao.GetAsync(
            $"/api/v1/pontos-agua/proximos?lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lng={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&limite=1"))
            .LerJsonAsync()).AsArray();

        // Assert
        Assert.Equal("Ponto com links", itens[0]!["ponto"]!["nome"]!.GetValue<string>());
        Assert.Contains("regiao", itens[0]!["ponto"]!.Rels());
    }

    [Fact]
    public async Task GetTimeline_ComEventos_RetornaLinkDaSolicitacaoEmCadaEvento()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var json = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}/timeline")).LerJsonAsync();

        // Assert
        Assert.Contains("solicitacao", json["items"]![0]!.Rels());
        Assert.Equal(HttpStatusCode.OK, (await cidadao.GetAsync($"/api/v1/solicitacoes/{id}")).StatusCode);
    }
}
