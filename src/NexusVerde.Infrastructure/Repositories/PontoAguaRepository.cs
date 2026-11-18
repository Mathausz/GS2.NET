using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class PontoAguaRepository(NexusVerdeDbContext context) : Repository<PontoAgua>(context), IPontoAguaRepository
{
    private static readonly Dictionary<string, Func<IQueryable<PontoAgua>, bool, IOrderedQueryable<PontoAgua>>> Campos = new()
    {
        ["nome"] = (q, d) => d ? q.OrderByDescending(x => x.Nome) : q.OrderBy(x => x.Nome),
        ["capacidade"] = (q, d) => d ? q.OrderByDescending(x => x.CapacidadeLitrosDia) : q.OrderBy(x => x.CapacidadeLitrosDia),
        ["status"] = (q, d) => d ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
    };

    public Task<PagedResult<PontoAgua>> GetPagedAsync(PontoAguaQuery query, CancellationToken ct = default)
    {
        IQueryable<PontoAgua> q = Set.AsNoTracking();

        if (query.RegiaoId.HasValue) q = q.Where(x => x.RegiaoId == query.RegiaoId.Value);
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var busca = query.Busca.Trim().ToLower();
            q = q.Where(x => x.Nome.ToLower().Contains(busca) || x.Endereco.ToLower().Contains(busca));
        }

        return q.Ordenar(query, Campos, x => x.OrderBy(p => p.Nome).ThenBy(p => p.Id)).ToPagedAsync(query, ct);
    }

    public async Task<IReadOnlyList<PontoAgua>> ListarAtivosAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(x => x.Status == StatusPontoAgua.Ativo).ToListAsync(ct);
}
