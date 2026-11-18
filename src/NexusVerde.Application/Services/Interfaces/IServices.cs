using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Services.Interfaces;

public interface IAuthService
{
    Task<UsuarioResponse> RegistrarAsync(RegistroRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public interface IUsuarioService
{
    Task<PagedResult<UsuarioResponse>> ListarAsync(UsuarioQuery query, CancellationToken ct = default);
    Task<UsuarioResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<UsuarioResponse> CriarAsync(UsuarioCreateRequest request, CancellationToken ct = default);
    Task DesativarAsync(Guid id, Guid adminId, CancellationToken ct = default);
}

public interface IRegiaoService
{
    Task<PagedResult<RegiaoResponse>> ListarAsync(RegiaoQuery query, CancellationToken ct = default);
    Task<RegiaoResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<RegiaoResponse> CriarAsync(RegiaoRequest request, CancellationToken ct = default);
    Task<RegiaoResponse> AtualizarAsync(Guid id, RegiaoRequest request, CancellationToken ct = default);
    Task RemoverAsync(Guid id, CancellationToken ct = default);
}

public interface IDesastreService
{
    Task<PagedResult<DesastreResponse>> ListarAsync(DesastreQuery query, CancellationToken ct = default);
    Task<DesastreResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<DesastreResponse> CriarAsync(DesastreRequest request, CancellationToken ct = default);
    Task<DesastreResponse> AtualizarAsync(Guid id, DesastreUpdateRequest request, CancellationToken ct = default);
    Task RemoverAsync(Guid id, CancellationToken ct = default);
}

public interface IPontoAguaService
{
    Task<PagedResult<PontoAguaResponse>> ListarAsync(PontoAguaQuery query, CancellationToken ct = default);
    Task<PontoAguaResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PontoAguaProximoResponse>> ProximosAsync(double latitude, double longitude, int limite, CancellationToken ct = default);
    Task<PontoAguaResponse> CriarAsync(PontoAguaRequest request, CancellationToken ct = default);
    Task<PontoAguaResponse> AtualizarAsync(Guid id, PontoAguaRequest request, CancellationToken ct = default);
    Task<PontoAguaResponse> AlterarStatusAsync(Guid id, StatusPontoAgua status, CancellationToken ct = default);
    Task RemoverAsync(Guid id, CancellationToken ct = default);
}

public interface ISolicitacaoService
{
    Task<SolicitacaoCriadaResponse> CriarAsync(UsuarioContexto usuario, SolicitacaoRequest request, CancellationToken ct = default);
    Task<PagedResult<SolicitacaoResponse>> ListarMinhasAsync(UsuarioContexto usuario, SolicitacaoQuery query, CancellationToken ct = default);
    Task<PagedResult<SolicitacaoResponse>> ListarFilaAsync(UsuarioContexto usuario, SolicitacaoQuery query, CancellationToken ct = default);
    Task<SolicitacaoResponse> ObterAsync(UsuarioContexto usuario, Guid id, CancellationToken ct = default);
    Task<SolicitacaoResponse> CancelarAsync(UsuarioContexto usuario, Guid id, CancellationToken ct = default);
}

public interface IAtendimentoService
{
    Task<AtendimentoResponse> CriarAsync(UsuarioContexto usuario, AtendimentoRequest request, CancellationToken ct = default);
    Task<AtendimentoResponse> AtualizarStatusAsync(UsuarioContexto usuario, Guid id, AtendimentoStatusRequest request, CancellationToken ct = default);
    Task<PagedResult<AtendimentoResponse>> ListarAsync(UsuarioContexto usuario, AtendimentoQuery query, CancellationToken ct = default);
    Task<AtendimentoResponse> ObterAsync(UsuarioContexto usuario, Guid id, CancellationToken ct = default);
}

public interface IOrientacaoService
{
    Task<PagedResult<OrientacaoResponse>> ListarAsync(OrientacaoQuery query, CancellationToken ct = default);
    Task<OrientacaoResponse> ObterAsync(Guid id, CancellationToken ct = default);
    Task<OrientacaoResponse> CriarAsync(OrientacaoRequest request, CancellationToken ct = default);
    Task<OrientacaoResponse> AtualizarAsync(Guid id, OrientacaoRequest request, CancellationToken ct = default);
    Task RemoverAsync(Guid id, CancellationToken ct = default);
}

public interface ITimelineService
{
    /// <summary>Grava um evento na linha do tempo sem nunca interromper o fluxo principal (falhas viram log e métrica).</summary>
    Task RegistrarAsync(EventoTimeline evento, CancellationToken ct = default);

    Task<PagedResult<TimelineEventoResponse>> ListarAsync(UsuarioContexto usuario, Guid solicitacaoId, TimelineQuery query, CancellationToken ct = default);

    Task<TimelineEventoResponse> AdicionarNotaAsync(UsuarioContexto usuario, Guid solicitacaoId, NotaRequest request, CancellationToken ct = default);
}
