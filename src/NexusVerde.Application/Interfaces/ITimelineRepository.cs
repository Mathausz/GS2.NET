using NexusVerde.Application.Common;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Interfaces;

/// <summary>Armazena e consulta a linha do tempo das solicitações (tabela NV_TIMELINE).</summary>
public interface ITimelineRepository
{
    Task AddAsync(EventoTimeline evento, CancellationToken ct = default);

    Task<PagedResult<EventoTimeline>> ListarPorSolicitacaoAsync(Guid solicitacaoId, TimelineQuery query, CancellationToken ct = default);
}
