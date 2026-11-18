using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NexusVerde.Infrastructure.Data;

/// <summary>
/// Decide se a API usa o Oracle ou o banco em memória (EF InMemory).
/// <list type="bullet">
/// <item><c>Database:UseInMemory=true</c> → memória (demo/testes).</item>
/// <item><c>Database:UseInMemory=false</c> → Oracle (connection string obrigatória).</item>
/// <item>Não informado → memória apenas em <b>Development</b> sem <c>ConnectionStrings:Oracle</c>;
/// em qualquer outro caso, Oracle. Assim <c>dotnet run</c> funciona sem configuração e produção nunca cai
/// em memória por engano.</item>
/// </list>
/// </summary>
public static class ModoPersistencia
{
    public static bool UsarEmMemoria(IConfiguration config, IHostEnvironment environment)
    {
        if (bool.TryParse(config["Database:UseInMemory"], out var explicito))
            return explicito;

        return environment.IsDevelopment() && string.IsNullOrWhiteSpace(config.GetConnectionString("Oracle"));
    }

    public static string NomeBancoEmMemoria(IConfiguration config) =>
        string.IsNullOrWhiteSpace(config["Database:InMemoryName"]) ? "NexusVerde" : config["Database:InMemoryName"]!;
}
