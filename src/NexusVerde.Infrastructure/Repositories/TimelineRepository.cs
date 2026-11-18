using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

/// <summary>Linha do tempo das solicitações na tabela NV_TIMELINE (Oracle via EF Core).</summary>
public sealed class TimelineRepository(NexusVerdeDbContext context) : ITimelineRepository
{
    public async Task AddAsync(EventoTimeline evento, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(evento.Id))
            evento.Id = Guid.NewGuid().ToString("D");

        var entrada = context.Timeline.Add(evento);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch
        {
            // Não deixa o evento pendente no contexto: um SaveChanges posterior não deve tentar regravá-lo.
            entrada.State = EntityState.Detached;
            throw;
        }
    }

    public async Task<PagedResult<EventoTimeline>> ListarPorSolicitacaoAsync(
        Guid solicitacaoId, TimelineQuery query, CancellationToken ct = default)
    {
        var consulta = context.Timeline.AsNoTracking().Where(e => e.SolicitacaoId == solicitacaoId);
        if (query.Tipo.HasValue)
            consulta = consulta.Where(e => e.Tipo == query.Tipo.Value);

        // OcorridoEm é estritamente crescente na criação; o Id só desempata dados antigos/importados.
        consulta = query.Decrescente()
            ? consulta.OrderByDescending(e => e.OcorridoEm).ThenByDescending(e => e.Id)
            : consulta.OrderBy(e => e.OcorridoEm).ThenBy(e => e.Id);

        var pagina = query.PaginaEfetiva();
        var tamanho = query.TamanhoEfetivo();

        var total = await consulta.CountAsync(ct);
        var itens = await consulta.Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync(ct);

        return new PagedResult<EventoTimeline>(itens, pagina, tamanho, total);
    }
}
