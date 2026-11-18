using NexusVerde.Api.Security;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Api.Extensions;

/// <summary>Aplica migrations/seed na subida da aplicação (controlado por configuração).</summary>
public sealed class DatabaseInitializerHostedService(
    IServiceProvider services,
    ILogger<DatabaseInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await DatabaseInitializer.InicializarAsync(services, cancellationToken);

        if (ChaveJwtDesenvolvimento.Utilizada)
            logger.LogWarning("Jwt:Key não configurada: usando uma chave TEMPORÁRIA de desenvolvimento " +
                              "(tokens deixam de valer ao reiniciar). Defina Jwt__Key para uma chave fixa.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
