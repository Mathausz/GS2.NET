using Microsoft.AspNetCore.Http;
using NexusVerde.Api.Middleware;

namespace NexusVerde.Tests.Integration.Middleware;

/// <summary>Regras puras da idempotência (normalização da rota e expiração da chave), sem subir a API.</summary>
public class IdempotencyMiddlewareRulesTests
{
    [Theory]
    [InlineData("POST", "/api/v1/solicitacoes")]
    [InlineData("POST", "/api/v1/solicitacoes/")]
    [InlineData("post", "/API/v1/Solicitacoes")]
    [InlineData("POST", "/api/v1/solicitacoes//")]
    public void MontarRota_VariacoesDaMesmaRota_GeramAMesmaChave(string metodo, string caminho)
    {
        // Act
        var rota = IdempotencyMiddleware.MontarRota(metodo, new PathString(caminho));

        // Assert
        Assert.Equal("POST /api/v1/solicitacoes", rota);
    }

    [Fact]
    public void MontarRota_RotasDiferentes_GeramChavesDiferentes()
    {
        // Act
        var solicitacoes = IdempotencyMiddleware.MontarRota("POST", new PathString("/api/v1/solicitacoes"));
        var atendimentos = IdempotencyMiddleware.MontarRota("POST", new PathString("/api/v1/atendimentos"));

        // Assert
        Assert.NotEqual(solicitacoes, atendimentos);
    }

    [Fact]
    public void MontarRota_CaminhoMuitoLongo_LimitaA200Caracteres()
    {
        // Act
        var rota = IdempotencyMiddleware.MontarRota("POST", new PathString("/" + new string('a', 500)));

        // Assert
        Assert.Equal(200, rota.Length);
    }

    [Theory]
    [InlineData(-25, 24, true)]
    [InlineData(-23, 24, false)]
    [InlineData(-1000, 0, false)]
    public void Expirado_ConformeIdadeERetencao(int horasAtras, int retencao, bool esperado)
    {
        // Act
        var expirado = IdempotencyMiddleware.Expirado(DateTime.UtcNow.AddHours(horasAtras), retencao);

        // Assert
        Assert.Equal(esperado, expirado);
    }
}
