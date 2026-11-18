using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusVerde.Application.Interfaces;

namespace NexusVerde.Tests.Integration.Fixtures;

/// <summary>Serviço de clima falso: sem rede, resultado determinístico.</summary>
public sealed class FakeWeatherClient(double chuvaMm = 20, bool disponivel = true) : IWeatherClient
{
    public Task<ClimaPrevisto?> ObterPrevisaoAsync(double latitude, double longitude, CancellationToken ct = default) =>
        Task.FromResult<ClimaPrevisto?>(new ClimaPrevisto(chuvaMm));

    public Task<bool> VerificarDisponibilidadeAsync(CancellationToken ct = default) => Task.FromResult(disponivel);
}

/// <summary>
/// Sobe a API em memória (EF InMemory, inclusive a linha do tempo), com chave JWT de teste, admin inicial
/// criado pelo seed de configuração e clima falso. É compartilhada pelos testes via Collection Fixture.
/// </summary>
public class NexusVerdeFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@nexusverde.test";
    public const string AdminSenha = "Admin#Teste123";
    public const string JwtKey = "chave-secreta-somente-para-testes-0123456789";
    public const string JwtIssuer = "NexusVerde.Tests";
    public const string JwtAudience = "NexusVerde.Tests.Clients";

    private readonly string _bancoEmMemoria = $"nexusverde-{Guid.NewGuid():N}";
    private readonly Dictionary<string, string?> _extras;

    public NexusVerdeFactory() : this(new Dictionary<string, string?>()) { }

    protected NexusVerdeFactory(Dictionary<string, string?> extras) => _extras = extras;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var valores = new Dictionary<string, string?>
            {
                ["Database:UseInMemory"] = "true",
                ["Database:InMemoryName"] = _bancoEmMemoria,
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Database:RunSeedOnStartup"] = "false",
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminSenha,
                // Limites altos por padrão: os testes de fluxo fazem muitos logins. O 429 é testado à parte.
                ["RateLimiting:Global:PermitLimit"] = "100000",
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Criacao:PermitLimit"] = "100000"
            };

            foreach (var extra in _extras) valores[extra.Key] = extra.Value;
            config.AddInMemoryCollection(valores);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWeatherClient>();
            services.AddSingleton<IWeatherClient>(CriarClima());
        });
    }

    /// <summary>Serviço de clima usado pela API nos testes (sobrescrito para simular o Open-Meteo fora do ar).</summary>
    protected virtual IWeatherClient CriarClima() => new FakeWeatherClient();
}

/// <summary>Factory com o Open-Meteo "fora do ar", para exercitar o health check Degraded.</summary>
public sealed class ClimaIndisponivelFactory : NexusVerdeFactory
{
    protected override IWeatherClient CriarClima() => new FakeWeatherClient(disponivel: false);
}

/// <summary>Factory com limite baixo na política de criação (POST /solicitacoes), para exercitar o HTTP 429.</summary>
public sealed class CriacaoLimitadaFactory() : NexusVerdeFactory(new Dictionary<string, string?>
{
    ["RateLimiting:Criacao:PermitLimit"] = "2",
    ["RateLimiting:Criacao:WindowSeconds"] = "60"
});

/// <summary>
/// Sobe a API como o professor faria com <c>dotnet run</c>: ambiente Development e NENHUMA configuração de banco,
/// JWT ou admin. Os valores vazios abaixo só neutralizam user-secrets/variáveis que existam na máquina.
/// </summary>
public sealed class ZeroConfigFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Oracle"] = "",
                ["Database:UseInMemory"] = "",
                ["Database:InMemoryName"] = $"nexusverde-zero-{Guid.NewGuid():N}",
                ["Jwt:Key"] = "",
                ["Seed:AdminEmail"] = "",
                ["Seed:AdminPassword"] = ""
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWeatherClient>();
            services.AddSingleton<IWeatherClient>(new FakeWeatherClient());
        });
    }
}

/// <summary>Factory com limite de login muito baixo, para exercitar o HTTP 429.</summary>
public sealed class RateLimitedFactory() : NexusVerdeFactory(new Dictionary<string, string?>
{
    ["RateLimiting:Auth:PermitLimit"] = "3",
    ["RateLimiting:Auth:WindowSeconds"] = "60"
});
