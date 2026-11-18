using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class DesastreRepository(NexusVerdeDbContext context) : Repository<Desastre>(context), IDesastreRepository
{
    private static readonly Dictionary<string, Func<IQueryable<Desastre>, bool, IOrderedQueryable<Desastre>>> Campos = new()
    {
        ["gravidade"] = (q, d) => d ? q.OrderByDescending(x => x.Gravidade) : q.OrderBy(x => x.Gravidade),
        ["tipo"] = (q, d) => d ? q.OrderByDescending(x => x.Tipo) : q.OrderBy(x => x.Tipo),
        ["status"] = (q, d) => d ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
        ["inicio"] = (q, d) => d ? q.OrderByDescending(x => x.IniciadoEm) : q.OrderBy(x => x.IniciadoEm),
    };

    public Task<PagedResult<Desastre>> GetPagedAsync(DesastreQuery query, CancellationToken ct = default)
    {
        IQueryable<Desastre> q = Set.AsNoTracking();

        if (query.RegiaoId.HasValue) q = q.Where(x => x.RegiaoId == query.RegiaoId.Value);
        if (query.Tipo.HasValue) q = q.Where(x => x.Tipo == query.Tipo.Value);
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);

        return q.Ordenar(query, Campos, x => x.OrderByDescending(d => d.Gravidade).ThenBy(d => d.Id)).ToPagedAsync(query, ct);
    }

    public Task<Desastre?> GetMaisGraveAtivoAsync(Guid regiaoId, CancellationToken ct = default) =>
        Set.AsNoTracking()
            .Where(x => x.RegiaoId == regiaoId && x.Status != StatusDesastre.Encerrado)
            .OrderByDescending(x => x.Gravidade)
            .ThenByDescending(x => x.IniciadoEm)
            .FirstOrDefaultAsync(ct);
}
