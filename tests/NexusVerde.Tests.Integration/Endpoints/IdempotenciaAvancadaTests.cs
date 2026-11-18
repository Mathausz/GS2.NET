using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusVerde.Infrastructure.Data;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>
/// Casos de borda da idempotência: rota com barra final, requisições simultâneas (reenvio do app após
/// queda de rede) e expiração da chave.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class IdempotenciaAvancadaTests(NexusVerdeFactory factory)
{
    [Fact]
    public async Task PostSolicitacoes_MesmaChaveComBarraFinal_DevolveARespostaOriginal()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var pedido = ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId);
        var chave = Guid.NewGuid().ToString();

        // Act
        var primeira = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);
        var segunda = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes/", pedido, chave);

        // Assert
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.True(segunda.Headers.Contains("Idempotency-Replayed"));
        var id1 = (await primeira.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
        var id2 = (await segunda.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
        Assert.Equal(id1, id2);
    }

    [Fact]
    public async Task PostSolicitacoes_CincoRequisicoesSimultaneasMesmaChave_CriamUmaUnicaSolicitacao()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var pedido = ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId);
        var chave = Guid.NewGuid().ToString();

        // Act
        var respostas = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave)));

        // Assert
        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Single(respostas, r => !r.Headers.Contains("Idempotency-Replayed"));

        var ids = new HashSet<string>();
        foreach (var r in respostas)
            ids.Add((await r.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>());
        Assert.Single(ids);

        var minhas = await (await cidadao.GetAsync("/api/v1/solicitacoes/minhas")).LerJsonAsync();
        Assert.Equal(1, minhas["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task PostSolicitacoes_ChaveExpirada_TrataComoNovaOperacao()
    {
        // Arrange
        var cenario = await factory.PrepararCriseAsync();
        var cidadao = await factory.ComoCidadaoAsync();
        var pedido = ApiTestHelpers.PedidoDeAjuda(cenario.RegiaoId);
        var chave = Guid.NewGuid().ToString();
        var primeira = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);
        var id1 = (await primeira.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();

        // Simula a passagem do tempo: o registro da chave fica mais antigo que a retenção (24h).
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexusVerdeDbContext>();
            var registro = await db.RegistrosIdempotencia.SingleAsync(r => r.Chave == chave);
            registro.CriadoEm = DateTime.UtcNow.AddDays(-2);
            await db.SaveChangesAsync();
        }

        // Act
        var segunda = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes", pedido, chave);

        // Assert
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.False(segunda.Headers.Contains("Idempotency-Replayed"));
        var id2 = (await segunda.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
        Assert.NotEqual(id1, id2);
    }
}
