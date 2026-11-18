using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class AtendimentoRepository(NexusVerdeDbContext context) : Repository<Atendimento>(context), IAtendimentoRepository
{
    private static readonly Dictionary<string, Func<IQueryable<Atendimento>, bool, IOrderedQueryable<Atendimento>>> Campos = new()
    {
        ["iniciadoem"] = (q, d) => d ? q.OrderByDescending(x => x.IniciadoEm) : q.OrderBy(x => x.IniciadoEm),
        ["status"] = (q, d) => d ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
    };

    public Task<PagedResult<Atendimento>> GetPagedAsync(AtendimentoQuery query, Guid? gestorId, CancellationToken ct = default)
    {
        IQueryable<Atendimento> q = Set.AsNoTracking();

        if (gestorId.HasValue) q = q.Where(x => x.GestorId == gestorId.Value);
        if (query.SolicitacaoId.HasValue) q = q.Where(x => x.SolicitacaoId == query.SolicitacaoId.Value);
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);

        return q.Ordenar(query, Campos, x => x.OrderByDescending(a => a.IniciadoEm).ThenBy(a => a.Id)).ToPagedAsync(query, ct);
    }
}
