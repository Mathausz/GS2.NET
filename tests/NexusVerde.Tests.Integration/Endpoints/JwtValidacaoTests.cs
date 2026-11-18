using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NexusVerde.Tests.Integration.Fixtures;
using NexusVerde.Tests.Integration.Helpers;

namespace NexusVerde.Tests.Integration.Endpoints;

/// <summary>
/// Validação do token JWT: só passa token assinado com a chave da API, do issuer/audience corretos e dentro
/// da validade. Os tokens são montados à mão para isolar cada regra.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class JwtValidacaoTests(NexusVerdeFactory factory)
{
    private static string GerarToken(string? chave = null, string? issuer = null, string? audience = null,
        DateTime? expiraEm = null)
    {
        var expira = expiraEm ?? DateTime.UtcNow.AddMinutes(30);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("role", "Admin"),
                new Claim("orgao", "Nenhum")
            }),
            Issuer = issuer ?? NexusVerdeFactory.JwtIssuer,
            Audience = audience ?? NexusVerdeFactory.JwtAudience,
            IssuedAt = expira.AddHours(-1),
            NotBefore = expira.AddHours(-1),
            Expires = expira,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave ?? NexusVerdeFactory.JwtKey)),
                SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task GetRegioes_TokenValidoMontadoComAChaveDaApi_Retorna200()
    {
        // Arrange
        var client = factory.ComToken(GerarToken());

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_TokenExpirado_Retorna401()
    {
        // Arrange
        var client = factory.ComToken(GerarToken(expiraEm: DateTime.UtcNow.AddMinutes(-10)));

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_TokenAssinadoComOutraChave_Retorna401()
    {
        // Arrange
        var client = factory.ComToken(GerarToken(chave: "outra-chave-qualquer-com-mais-de-32-caracteres!!"));

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_TokenDeOutroIssuer_Retorna401()
    {
        // Arrange
        var client = factory.ComToken(GerarToken(issuer: "Outro.Emissor"));

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRegioes_TokenParaOutraAudience_Retorna401()
    {
        // Arrange
        var client = factory.ComToken(GerarToken(audience: "Outra.Aplicacao"));

        // Act
        var response = await client.GetAsync("/api/v1/regioes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
