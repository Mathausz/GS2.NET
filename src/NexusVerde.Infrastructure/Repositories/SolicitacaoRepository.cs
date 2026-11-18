using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class SolicitacaoRepository(NexusVerdeDbContext context) : Repository<SolicitacaoAjuda>(context), ISolicitacaoRepository
{
    private static readonly Dictionary<string, Func<IQueryable<SolicitacaoAjuda>, bool, IOrderedQueryable<SolicitacaoAjuda>>> Campos = new()
    {
        ["score"] = (q, d) => d ? q.OrderByDescending(x => x.ScorePrioridade) : q.OrderBy(x => x.ScorePrioridade),
        ["criadoem"] = (q, d) => d ? q.OrderByDescending(x => x.CriadoEm) : q.OrderBy(x => x.CriadoEm),
        ["status"] = (q, d) => d ? q.OrderByDescending(x => x.Status) : q.OrderBy(x => x.Status),
        ["tipo"] = (q, d) => d ? q.OrderByDescending(x => x.Tipo) : q.OrderBy(x => x.Tipo),
    };

    public Task<PagedResult<SolicitacaoAjuda>> GetPagedAsync(SolicitacaoQuery query, Guid? cidadaoId,
        OrgaoResponsavel? orgaoRestrito, bool filaPadrao, CancellationToken ct = default)
    {
        IQueryable<SolicitacaoAjuda> q = Set.AsNoTracking();

        if (cidadaoId.HasValue) q = q.Where(x => x.CidadaoId == cidadaoId.Value);

        if (orgaoRestrito.HasValue)
            q = q.Where(x => x.OrgaoDestino == orgaoRestrito.Value);
        else if (query.OrgaoDestino.HasValue)
            q = q.Where(x => x.OrgaoDestino == query.OrgaoDestino.Value);

        if (query.RegiaoId.HasValue) q = q.Where(x => x.RegiaoId == query.RegiaoId.Value);
        if (query.Tipo.HasValue) q = q.Where(x => x.Tipo == query.Tipo.Value);
        if (query.Prioridade.HasValue) q = q.Where(x => x.NivelPrioridade == query.Prioridade.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);
        else if (filaPadrao)
            q = q.Where(x => x.Status == StatusSolicitacao.Aberta || x.Status == StatusSolicitacao.EmAtendimento);

        // Padrão: mais prioritárias primeiro e, em empate, as mais antigas.
        return q.Ordenar(query, Campos,
                x => x.OrderByDescending(s => s.ScorePrioridade).ThenBy(s => s.CriadoEm).ThenBy(s => s.Id))
            .ToPagedAsync(query, ct);
    }
}
