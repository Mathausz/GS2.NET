using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexusVerde.Infrastructure.Repositories;

public class RegiaoRepository(NexusVerdeDbContext context) : Repository<Regiao>(context), IRegiaoRepository
{
    private static readonly Dictionary<string, Func<IQueryable<Regiao>, bool, IOrderedQueryable<Regiao>>> Campos = new()
    {
        ["nome"] = (q, d) => d ? q.OrderByDescending(x => x.Nome) : q.OrderBy(x => x.Nome),
        ["municipio"] = (q, d) => d ? q.OrderByDescending(x => x.Municipio) : q.OrderBy(x => x.Municipio),
        ["estado"] = (q, d) => d ? q.OrderByDescending(x => x.Estado) : q.OrderBy(x => x.Estado),
        ["risco"] = (q, d) => d ? q.OrderByDescending(x => x.NivelRisco) : q.OrderBy(x => x.NivelRisco),
        ["populacao"] = (q, d) => d ? q.OrderByDescending(x => x.PopulacaoEstimada) : q.OrderBy(x => x.PopulacaoEstimada),
        ["criadoem"] = (q, d) => d ? q.OrderByDescending(x => x.CriadoEm) : q.OrderBy(x => x.CriadoEm),
    };

    public Task<PagedResult<Regiao>> GetPagedAsync(RegiaoQuery query, CancellationToken ct = default)
    {
        IQueryable<Regiao> q = Set.AsNoTracking();

        if (query.NivelRisco.HasValue) q = q.Where(x => x.NivelRisco == query.NivelRisco.Value);
        if (!string.IsNullOrWhiteSpace(query.Estado))
        {
            var uf = query.Estado.Trim().ToUpper();
            q = q.Where(x => x.Estado == uf);
        }
        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var busca = query.Busca.Trim().ToLower();
            q = q.Where(x => x.Nome.ToLower().Contains(busca) || x.Municipio.ToLower().Contains(busca));
        }

        return q.Ordenar(query, Campos, x => x.OrderBy(r => r.Nome).ThenBy(r => r.Id)).ToPagedAsync(query, ct);
    }
}
