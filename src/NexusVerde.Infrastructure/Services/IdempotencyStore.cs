using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Interfaces;
using NexusVerde.Domain.Entities;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Services;

public sealed class IdempotencyStore(NexusVerdeDbContext context) : IIdempotencyStore
{
    public Task<RegistroIdempotencia?> BuscarAsync(string chave, Guid usuarioId, string rota, CancellationToken ct = default) =>
        context.RegistrosIdempotencia.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Chave == chave && x.UsuarioId == usuarioId && x.Rota == rota, ct);

    public async Task SalvarAsync(RegistroIdempotencia registro, CancellationToken ct = default)
    {
        context.RegistrosIdempotencia.Add(registro);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Outra requisição com a mesma chave gravou primeiro (índice único): nada a fazer.
            context.Entry(registro).State = EntityState.Detached;
        }
    }

    public async Task RemoverAsync(RegistroIdempotencia registro, CancellationToken ct = default)
    {
        context.RegistrosIdempotencia.Remove(registro);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Já removido por outra requisição: nada a fazer.
            context.Entry(registro).State = EntityState.Detached;
        }
    }
}
