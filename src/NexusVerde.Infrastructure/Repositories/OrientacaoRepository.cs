using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class OrientacaoRepository(NexusVerdeDbContext context) : Repository<Orientacao>(context), IOrientacaoRepository
{
    private static readonly Dictionary<string, Func<IQueryable<Orientacao>, bool, IOrderedQueryable<Orientacao>>> Campos = new()
    {
        ["ordem"] = (q, d) => d ? q.OrderByDescending(x => x.Ordem) : q.OrderBy(x => x.Ordem),
        ["titulo"] = (q, d) => d ? q.OrderByDescending(x => x.Titulo) : q.OrderBy(x => x.Titulo),
    };

    public Task<PagedResult<Orientacao>> GetPagedAsync(OrientacaoQuery query, CancellationToken ct = default)
    {
        IQueryable<Orientacao> q = Set.AsNoTracking();
        if (query.Tipo.HasValue) q = q.Where(x => x.Tipo == query.Tipo.Value);

        return q.Ordenar(query, Campos, x => x.OrderBy(o => o.Ordem).ThenBy(o => o.Id)).ToPagedAsync(query, ct);
    }

    public async Task<IReadOnlyList<Orientacao>> ListarPorTipoAsync(TipoSolicitacao tipo, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(x => x.Tipo == tipo).OrderBy(x => x.Ordem).ToListAsync(ct);
}
