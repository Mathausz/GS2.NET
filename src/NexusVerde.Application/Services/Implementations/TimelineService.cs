using Microsoft.Extensions.Logging;
using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Application.Telemetry;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Application.Services.Implementations;

/// <summary>
/// Linha do tempo das solicitações (tabela NV_TIMELINE). A gravação dos eventos automáticos é
/// "melhor esforço": o pedido já foi salvo, então uma falha ao gravar o evento é registrada em log e
/// métrica, mas não desfaz a operação principal.
/// </summary>
public class TimelineService(
    ITimelineRepository timeline,
    ISolicitacaoRepository solicitacoes,
    ILogger<TimelineService> logger) : ITimelineService
{
    public async Task RegistrarAsync(EventoTimeline evento, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Timeline.Registrar");
        activity?.SetTag("timeline.tipo", evento.Tipo.ToString());

        try
        {
            await timeline.AddAsync(evento, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            NexusVerdeTelemetry.TimelineFalhas.Add(1);
            logger.LogError(ex,
                "Falha ao gravar o evento {Tipo} da solicitação {SolicitacaoId} na linha do tempo; o fluxo principal segue.",
                evento.Tipo, evento.SolicitacaoId);
        }
    }

    public async Task<PagedResult<TimelineEventoResponse>> ListarAsync(
        UsuarioContexto usuario, Guid solicitacaoId, TimelineQuery query, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Timeline.Listar");

        var solicitacao = await solicitacoes.GetByIdAsync(solicitacaoId, ct)
                          ?? throw new NotFoundException("Solicitação não encontrada.");

        if (!SolicitacaoAcesso.Pode(usuario, solicitacao))
            throw new ForbiddenException("Você não tem acesso à linha do tempo desta solicitação.");

        try
        {
            var pagina = await timeline.ListarPorSolicitacaoAsync(solicitacaoId, query, ct);
            return pagina.Map(e => e.ToResponse());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao consultar a linha do tempo da solicitação {SolicitacaoId}.", solicitacaoId);
            throw new ExternalServiceException("A linha do tempo está indisponível no momento. Tente novamente em instantes.");
        }
    }

    public async Task<TimelineEventoResponse> AdicionarNotaAsync(
        UsuarioContexto usuario, Guid solicitacaoId, NotaRequest request, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Timeline.AdicionarNota");

        var solicitacao = await solicitacoes.GetByIdAsync(solicitacaoId, ct)
                          ?? throw new NotFoundException("Solicitação não encontrada.");

        if (!usuario.PodeVerOrgao(solicitacao.OrgaoDestino))
            throw new ForbiddenException("Esta solicitação pertence a outro órgão responsável.");

        if (solicitacao.Status == StatusSolicitacao.Cancelada)
            throw new DomainException("Não é possível anotar em uma solicitação cancelada.");

        var evento = EventoTimeline.Criar(solicitacaoId, TipoEventoTimeline.NotaAdicionada, request.Texto,
            usuario.Id, usuario.Perfil, usuario.Orgao);

        try
        {
            await timeline.AddAsync(evento, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao gravar a nota da solicitação {SolicitacaoId}.", solicitacaoId);
            throw new ExternalServiceException("Não foi possível registrar a nota agora. Tente novamente em instantes.");
        }

        return evento.ToResponse();
    }
}
