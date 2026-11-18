using System.Net;
using System.Net.Http.Json;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>Fluxo principal do produto, ponta a ponta: crise → pedido de ajuda → fila → atendimento.</summary>
[Collection(IntegrationTestCollection.Name)]
public class SolicitacaoFlowTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task PostDesastre_GravidadeQuatro_RecalculaRiscoDaRegiaoParaAlto()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        var (lat, lng) = ApiTestHelpers.NovaCoordenada();
        var regiaoId = await admin.CriarRegiaoAsync($"Risco {Guid.NewGuid():N}", "RS", lat, lng);

        // Act
        var resposta = await admin.PostAsJsonAsync("/api/v1/desastres", new
        {
            regiaoId, tipo = "Enchente", gravidade = 4, descricao = "Alagamento severo na região."
        });
        var regiao = await (await admin.GetAsync($"/api/v1/regioes/{regiaoId}")).LerJsonAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("Alto", regiao["nivelRisco"]!.GetValue<string>());
    }

    [Fact]
    public async Task PostSolicitacoes_AguaSuspeita_RetornaPontoMaisProximoEOrientacoes()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        await cenario.Admin.CriarPontoAsync(cenario.RegiaoId, "Ponto distante", -80.0, 0.0);
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId, "AguaSuspeita"), Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.LerJsonAsync();
        Assert.Equal("Ginásio Central", json["pontoMaisProximo"]!["ponto"]!["nome"]!.GetValue<string>());
        Assert.Equal(cenario.PontoId, json["pontoMaisProximo"]!["ponto"]!["id"]!.GetValue<string>());
        Assert.True(json["orientacoes"]!.AsArray().Count >= 1);
        Assert.Equal("Aberta", json["solicitacao"]!["status"]!.GetValue<string>());
        Assert.Equal("DefesaCivil", json["solicitacao"]!["orgaoDestino"]!.GetValue<string>());
        Assert.True(json["solicitacao"]!["scorePrioridade"]!.GetValue<double>() > 0);
    }

    [Fact]
    public async Task PostSolicitacoes_SemIdempotencyKey_Retorna400()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId), chave: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitacoes_MesmaChaveRepetida_NaoDuplicaEDevolveAMesmaResposta()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var chave = Guid.NewGuid().ToString();
        var pedido = ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId);

        // Act
        var primeira = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);
        var segunda = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);

        // Assert
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.False(primeira.Headers.Contains("Idempotency-Replayed"));
        Assert.True(segunda.Headers.Contains("Idempotency-Replayed"));
        var idPrimeira = (await primeira.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
        var idSegunda = (await segunda.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
        Assert.Equal(idPrimeira, idSegunda);
        var minhas = await (await cidadao.GetAsync("/api/v1/solicitacoes/minhas")).LerJsonAsync();
        Assert.Equal(1, minhas["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task PostSolicitacoes_MesmaChaveComCorpoDiferente_Retorna422()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var chave = Guid.NewGuid().ToString();
        await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId, pessoas: 5), chave);

        // Act
        var conflito = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId, pessoas: 50), chave);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, conflito.StatusCode);
    }

    [Fact]
    public async Task PostSolicitacoes_ChavesDiferentes_CriamSolicitacoesDistintas()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Assert
        var minhas = await (await cidadao.GetAsync("/api/v1/solicitacoes/minhas")).LerJsonAsync();
        Assert.Equal(2, minhas["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task PostSolicitacoes_DadosInvalidos_Retorna400()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            new { regiaoId = cenario.RegiaoId, tipo = "SemAgua", descricao = "x", quantidadePessoas = 0, horasSemAgua = 1 },
            Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitacoes_RegiaoInexistente_Retorna404()
    {
        // Arrange
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            ApiTestHelpers.PedidoDeAjuda(Guid.NewGuid().ToString()), Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FluxoCompleto_GestorDoOrgaoAssumeEConclui_SolicitacaoFicaAtendida()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var solicitacaoId = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var fila = await (await gestor.GetAsync($"/api/v1/solicitacoes/fila?regiaoId={cenario.RegiaoId}")).LerJsonAsync();
        var atendimento = await gestor.PostIdempotenteAsync("/api/v1/atendimentos",
            new { solicitacaoId, observacao = "Caminhão-pipa a caminho" }, Guid.NewGuid().ToString());
        var atendimentoId = (await atendimento.LerJsonAsync())["id"]!.GetValue<string>();
        var emAtendimento = await (await gestor.GetAsync($"/api/v1/solicitacoes/{solicitacaoId}")).LerJsonAsync();
        var concluido = await gestor.PatchAsJsonAsync($"/api/v1/atendimentos/{atendimentoId}/status",
            new { status = "Concluido", observacao = "Água entregue" });
        var final = await (await cidadao.GetAsync($"/api/v1/solicitacoes/{solicitacaoId}")).LerJsonAsync();

        // Assert
        Assert.Equal(1, fila["totalItems"]!.GetValue<int>());
        Assert.Equal(HttpStatusCode.Created, atendimento.StatusCode);
        Assert.Equal("EmAtendimento", emAtendimento["status"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.OK, concluido.StatusCode);
        Assert.Equal("Atendida", final["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetFila_AcessoNegado_VaiParaPoliciaENaoApareceParaPrefeitura()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var prefeitura = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var policia = await factory.ComoGestorAsync(cenario.Admin, "Policia");
        var solicitacaoId = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId, "AcessoNegado");

        // Act
        var filaPrefeitura = await (await prefeitura.GetAsync($"/api/v1/solicitacoes/fila?regiaoId={cenario.RegiaoId}")).LerJsonAsync();
        var filaPolicia = await (await policia.GetAsync($"/api/v1/solicitacoes/fila?regiaoId={cenario.RegiaoId}")).LerJsonAsync();
        var detalhePrefeitura = await prefeitura.GetAsync($"/api/v1/solicitacoes/{solicitacaoId}");

        // Assert
        Assert.Equal(0, filaPrefeitura["totalItems"]!.GetValue<int>());
        Assert.Equal(1, filaPolicia["totalItems"]!.GetValue<int>());
        Assert.Equal(HttpStatusCode.Forbidden, detalhePrefeitura.StatusCode);
    }

    [Fact]
    public async Task GetSolicitacao_OutroCidadao_Retorna403()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var autor = await factory.ComoCidadaoAsync();
        var outro = await factory.ComoCidadaoAsync();
        var id = await autor.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await outro.GetAsync($"/api/v1/solicitacoes/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchCancelar_SolicitacaoAbertaDoAutor_MudaStatusParaCancelada()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var id = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);

        // Act
        var response = await cidadao.PatchAsJsonAsync($"/api/v1/solicitacoes/{id}/cancelar", new { });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cancelada", (await response.LerJsonAsync())["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task PostAtendimentos_PontoContaminado_Retorna422()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var gestor = await factory.ComoGestorAsync(cenario.Admin, "Prefeitura");
        var solicitacaoId = await cidadao.CriarSolicitacaoAsync(cenario.RegiaoId);
        await gestor.PatchAsJsonAsync($"/api/v1/pontos-agua/{cenario.PontoId}/status", new { status = "Contaminado" });

        // Act
        var response = await gestor.PostIdempotenteAsync("/api/v1/atendimentos",
            new { solicitacaoId, pontoAguaId = cenario.PontoId }, Guid.NewGuid().ToString());

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetPontosProximos_DoisPontos_OrdenaPorDistancia()
    {
        // Arrange
        var admin = await factory.ComoAdminAsync();
        var regiaoId = await admin.CriarRegiaoAsync($"Prox {Guid.NewGuid():N}");
        await admin.CriarPontoAsync(regiaoId, "Longe", -29.0, -51.0);
        await admin.CriarPontoAsync(regiaoId, "Perto", -30.0100, -51.1300);
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.GetAsync("/api/v1/pontos-agua/proximos?lat=-30.0098&lng=-51.1301&limite=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var itens = (await response.LerJsonAsync()).AsArray();
        Assert.Equal("Perto", itens[0]!["ponto"]!["nome"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetPontosProximos_CoordenadasInvalidas_Retorna400()
    {
        // Arrange
        var cidadao = await factory.ComoCidadaoAsync();

        // Act
        var response = await cidadao.GetAsync("/api/v1/pontos-agua/proximos?lat=200&lng=-51");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
