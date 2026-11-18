using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NexusVerde.Infrastructure.Data;

/// <summary>
/// Usada apenas em tempo de design (dotnet ef migrations ...).
/// Lê a connection string da variável de ambiente ConnectionStrings__Oracle; nunca fica em código.
/// </summary>
public class NexusVerdeDbContextFactory : IDesignTimeDbContextFactory<NexusVerdeDbContext>
{
    public NexusVerdeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Oracle")
                               ?? "Data Source=localhost:1521/ORCL"; // apenas design-time: sem usuario/senha

        var options = new DbContextOptionsBuilder<NexusVerdeDbContext>()
            .UseOracle(connectionString, OracleCompatibilidade.Configurar)
            .Options;

        return new NexusVerdeDbContext(options);
    }
}
