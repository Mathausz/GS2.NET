using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Services.Implementations;

public class PontoAguaService(
    IPontoAguaRepository pontos,
    IRegiaoRepository regioes,
    IUnitOfWork unitOfWork) : IPontoAguaService
{
    public async Task<PagedResult<PontoAguaResponse>> ListarAsync(PontoAguaQuery query, CancellationToken ct = default) =>
        (await pontos.GetPagedAsync(query, ct)).Map(p => p.ToResponse());

    public async Task<PontoAguaResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        (await BuscarAsync(id, ct)).ToResponse();

    public async Task<IReadOnlyList<PontoAguaProximoResponse>> ProximosAsync(
        double latitude, double longitude, int limite, CancellationToken ct = default)
    {
        var ativos = await pontos.ListarAtivosAsync(ct);
        return ativos
            .Select(p => new PontoAguaProximoResponse(
                p.ToResponse(),
                Math.Round(GeoCalculator.DistanciaKm(latitude, longitude, p.Latitude, p.Longitude), 2)))
            .OrderBy(x => x.DistanciaKm)
            .Take(Math.Clamp(limite, 1, 20))
            .ToList();
    }

    public async Task<PontoAguaResponse> CriarAsync(PontoAguaRequest request, CancellationToken ct = default)
    {
        _ = await regioes.GetByIdAsync(request.RegiaoId, ct) ?? throw new NotFoundException("Região não encontrada.");

        var ponto = new PontoAgua(request.RegiaoId, request.Nome, request.Endereco,
            request.Latitude, request.Longitude, request.CapacidadeLitrosDia, request.Descricao);
        await pontos.AddAsync(ponto, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ponto.ToResponse();
    }

    public async Task<PontoAguaResponse> AtualizarAsync(Guid id, PontoAguaRequest request, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);
        ponto.Atualizar(request.Nome, request.Endereco, request.Latitude, request.Longitude,
            request.CapacidadeLitrosDia, request.Descricao);
        await unitOfWork.SaveChangesAsync(ct);
        return ponto.ToResponse();
    }

    public async Task<PontoAguaResponse> AlterarStatusAsync(Guid id, StatusPontoAgua status, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);
        ponto.AlterarStatus(status);
        await unitOfWork.SaveChangesAsync(ct);
        return ponto.ToResponse();
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var ponto = await BuscarAsync(id, ct);
        pontos.Remove(ponto);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<PontoAgua> BuscarAsync(Guid id, CancellationToken ct) =>
        await pontos.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ponto de água não encontrado.");
}
