using System.Globalization;
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

namespace NexusVerde.Application.Services.Implementations;

/// <summary>
/// Funcionalidade central do Nexus Verde: recebe o pedido de ajuda do cidadão, calcula a prioridade,
/// indica o ponto de água seguro mais próximo e devolve as orientações de segurança da situação.
/// </summary>
public class SolicitacaoService(
    ISolicitacaoRepository solicitacoes,
    IRegiaoRepository regioes,
    IDesastreRepository desastres,
    IPontoAguaRepository pontos,
    IOrientacaoRepository orientacoes,
    IWeatherClient weather,
    ITimelineService timeline,
    IUnitOfWork unitOfWork) : ISolicitacaoService
{
    public async Task<SolicitacaoCriadaResponse> CriarAsync(
        UsuarioContexto usuario, SolicitacaoRequest request, CancellationToken ct = default)
    {
        using var activity = NexusVerdeTelemetry.ActivitySource.StartActivity("Solicitacao.Criar");
        activity?.SetTag("solicitacao.tipo", request.Tipo.ToString());

        var regiao = await regioes.GetByIdAsync(request.RegiaoId, ct)
                     ?? throw new NotFoundException("Região não encontrada.");

        var latitude = request.Latitude ?? regiao.Latitude;
        var longitude = request.Longitude ?? regiao.Longitude;

        var desastre = await desastres.GetMaisGraveAtivoAsync(regiao.Id, ct);
        var clima = await weather.ObterPrevisaoAsync(latitude, longitude, ct);

        var prioridade = PrioridadeCalculator.Calcular(
            request.Tipo, desastre?.Gravidade ?? 0, request.QuantidadePessoas,
            request.HorasSemAgua, clima?.PrecipitacaoTotalMm);

        var maisProximo = await EncontrarPontoMaisProximoAsync(latitude, longitude, ct);

        var solicitacao = new SolicitacaoAjuda(
            usuario.Id, regiao.Id, request.Tipo, request.Descricao, request.QuantidadePessoas,
            request.HorasSemAgua, latitude, longitude, prioridade.Score, prioridade.Nivel,
            PrioridadeCalculator.OrgaoDestinoPorTipo(request.Tipo), desastre?.Id, maisProximo?.Ponto.Id);

        await solicitacoes.AddAsync(solicitacao, ct);
        await unitOfWork.SaveChangesAsync(ct);

        NexusVerdeTelemetry.SolicitacaoCriada(solicitacao.Tipo, solicitacao.NivelPrioridade);
        activity?.SetTag("solicitacao.prioridade", solicitacao.NivelPrioridade.ToString());

        await timeline.RegistrarAsync(EventoTimeline.Criar(
            solicitacao.Id, TipoEventoTimeline.Criada,
            $"Solicitação criada ({solicitacao.Tipo}) com prioridade {solicitacao.NivelPrioridade}.",
            usuario.Id, usuario.Perfil, usuario.Orgao,
            new Dictionary<string, string>
            {
                ["tipo"] = solicitacao.Tipo.ToString(),
                ["score"] = solicitacao.ScorePrioridade.ToString("0.0", CultureInfo.InvariantCulture),
                ["prioridade"] = solicitacao.NivelPrioridade.ToString(),
                ["orgaoDestino"] = solicitacao.OrgaoDestino.ToString(),
                ["regiaoId"] = solicitacao.RegiaoId.ToString(),
                ["pontoSugeridoId"] = solicitacao.PontoSugeridoId?.ToString() ?? string.Empty
            }), ct);

        var instrucoes = await orientacoes.ListarPorTipoAsync(request.Tipo, ct);

        return new SolicitacaoCriadaResponse(
            solicitacao.ToResponse(),
            maisProximo,
            instrucoes.Select(o => o.ToResponse()).ToList());
    }

    public async Task<PagedResult<SolicitacaoResponse>> ListarMinhasAsync(
        UsuarioContexto usuario, SolicitacaoQuery query, CancellationToken ct = default) =>
        (await solicitacoes.GetPagedAsync(query, usuario.Id, null, false, ct)).Map(s => s.ToResponse());

    public async Task<PagedResult<SolicitacaoResponse>> ListarFilaAsync(
        UsuarioContexto usuario, SolicitacaoQuery query, CancellationToken ct = default)
    {
        // Gestores de outros órgãos só enxergam o próprio; Admin e Defesa Civil enxergam tudo.
        var restrito = usuario.OrgaoRestrito;
        return (await solicitacoes.GetPagedAsync(query, null, restrito, true, ct)).Map(s => s.ToResponse());
    }

    public async Task<SolicitacaoResponse> ObterAsync(UsuarioContexto usuario, Guid id, CancellationToken ct = default)
    {
        var solicitacao = await solicitacoes.GetByIdAsync(id, ct)
                          ?? throw new NotFoundException("Solicitação não encontrada.");

        if (!SolicitacaoAcesso.Pode(usuario, solicitacao))
            throw new ForbiddenException("Você não tem acesso a esta solicitação.");

        return solicitacao.ToResponse();
    }

    public async Task<SolicitacaoResponse> CancelarAsync(UsuarioContexto usuario, Guid id, CancellationToken ct = default)
    {
        var solicitacao = await solicitacoes.GetByIdAsync(id, ct)
                          ?? throw new NotFoundException("Solicitação não encontrada.");

        if (solicitacao.CidadaoId != usuario.Id && !usuario.EhAdmin)
            throw new ForbiddenException("Somente o autor da solicitação pode cancelá-la.");

        solicitacao.Cancelar();
        await unitOfWork.SaveChangesAsync(ct);

        await timeline.RegistrarAsync(EventoTimeline.Criar(
            solicitacao.Id, TipoEventoTimeline.Cancelada, "Solicitação cancelada pelo cidadão.",
            usuario.Id, usuario.Perfil, usuario.Orgao), ct);

        return solicitacao.ToResponse();
    }

    private async Task<PontoAguaProximoResponse?> EncontrarPontoMaisProximoAsync(
        double latitude, double longitude, CancellationToken ct)
    {
        var ativos = await pontos.ListarAtivosAsync(ct);
        return ativos
            .Select(p => new PontoAguaProximoResponse(
                p.ToResponse(),
                Math.Round(GeoCalculator.DistanciaKm(latitude, longitude, p.Latitude, p.Longitude), 2)))
            .OrderBy(x => x.DistanciaKm)
            .FirstOrDefault();
    }
}
