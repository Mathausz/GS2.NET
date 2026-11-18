using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Application.Telemetry;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Application.Services.Implementations;

public class AtendimentoService(
    IAtendimentoRepository atendimentos,
    ISolicitacaoRepository solicitacoes,
    IPontoAguaRepository pontos,
    ITimelineService timeline,
    IUnitOfWork unitOfWork) : IAtendimentoService
{
    public async Task<AtendimentoResponse> CriarAsync(
        UsuarioContexto usuario, AtendimentoRequest request, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Atendimento.Criar");

        var solicitacao = await solicitacoes.GetByIdAsync(request.SolicitacaoId, ct)
                          ?? throw new NotFoundException("Solicitação não encontrada.");

        if (!usuario.PodeVerOrgao(solicitacao.OrgaoDestino))
            throw new ForbiddenException("Esta solicitação pertence a outro órgão responsável.");

        if (solicitacao.Status == StatusSolicitacao.EmAtendimento)
            throw new ConflictException("A solicitação já está em atendimento.");

        var pontoId = request.PontoAguaId ?? solicitacao.PontoSugeridoId;

        if (pontoId is null && solicitacao.Tipo == TipoSolicitacao.SemAgua)
            throw new DomainException("É necessário indicar um ponto de água para atender uma solicitação de falta de água.");

        if (pontoId is not null)
        {
            var ponto = await pontos.GetByIdAsync(pontoId.Value, ct)
                        ?? throw new NotFoundException("Ponto de água não encontrado.");

            if (!ponto.PodeAtender)
                throw new DomainException($"O ponto de água '{ponto.Nome}' não está disponível ({ponto.Status}).");
        }

        solicitacao.IniciarAtendimento();

        var atendimento = new Atendimento(solicitacao.Id, usuario.Id, pontoId, request.Observacao);
        await atendimentos.AddAsync(atendimento, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await timeline.RegistrarAsync(EventoTimeline.Criar(
            solicitacao.Id, TipoEventoTimeline.AtendimentoIniciado, "Atendimento iniciado pela equipe responsável.",
            usuario.Id, usuario.Perfil, usuario.Orgao,
            new Dictionary<string, string>
            {
                ["atendimentoId"] = atendimento.Id.ToString(),
                ["pontoAguaId"] = pontoId?.ToString() ?? string.Empty
            }), ct);

        return atendimento.ToResponse();
    }

    public async Task<AtendimentoResponse> AtualizarStatusAsync(
        UsuarioContexto usuario, Guid id, AtendimentoStatusRequest request, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Atendimento.AtualizarStatus");

        var atendimento = await atendimentos.GetByIdAsync(id, ct)
                          ?? throw new NotFoundException("Atendimento não encontrado.");

        if (atendimento.GestorId != usuario.Id && !usuario.EhAdmin)
            throw new ForbiddenException("Somente o gestor responsável pode atualizar este atendimento.");

        var solicitacao = await solicitacoes.GetByIdAsync(atendimento.SolicitacaoId, ct)
                          ?? throw new NotFoundException("Solicitação não encontrada.");

        switch (request.Status)
        {
            case StatusAtendimento.Concluido:
                atendimento.Concluir(request.Observacao);
                solicitacao.MarcarAtendida();
                break;
            case StatusAtendimento.Cancelado:
                atendimento.Cancelar(request.Observacao);
                solicitacao.RetornarParaAberta();
                break;
            default:
                throw new DomainException("Só é possível alterar o atendimento para Concluido ou Cancelado.");
        }

        await unitOfWork.SaveChangesAsync(ct);

        NexusVerdeTelemetry.AtendimentoFinalizado(atendimento.Status);
        var tipoEvento = atendimento.Status == StatusAtendimento.Concluido
            ? TipoEventoTimeline.AtendimentoConcluido
            : TipoEventoTimeline.AtendimentoCancelado;
        await timeline.RegistrarAsync(EventoTimeline.Criar(
            solicitacao.Id, tipoEvento,
            atendimento.Status == StatusAtendimento.Concluido ? "Atendimento concluído." : "Atendimento cancelado; a solicitação voltou para a fila.",
            usuario.Id, usuario.Perfil, usuario.Orgao,
            new Dictionary<string, string> { ["atendimentoId"] = atendimento.Id.ToString() }), ct);

        return atendimento.ToResponse();
    }

    public async Task<PagedResult<AtendimentoResponse>> ListarAsync(
        UsuarioContexto usuario, AtendimentoQuery query, CancellationToken ct = default)
    {
        // Gestor vê os próprios atendimentos; Admin vê todos.
        Guid? gestorId = usuario.EhAdmin ? null : usuario.Id;
        return (await atendimentos.GetPagedAsync(query, gestorId, ct)).Map(a => a.ToResponse());
    }

    public async Task<AtendimentoResponse> ObterAsync(UsuarioContexto usuario, Guid id, CancellationToken ct = default)
    {
        var atendimento = await atendimentos.GetByIdAsync(id, ct)
                          ?? throw new NotFoundException("Atendimento não encontrado.");

        if (atendimento.GestorId != usuario.Id && !usuario.EhAdmin)
            throw new ForbiddenException("Você não tem acesso a este atendimento.");

        return atendimento.ToResponse();
    }
}
