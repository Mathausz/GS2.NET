using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Services.Implementations;

public class RegiaoService(IRegiaoRepository regioes, IUnitOfWork unitOfWork) : IRegiaoService
{
    public async Task<PagedResult<RegiaoResponse>> ListarAsync(RegiaoQuery query, CancellationToken ct = default) =>
        (await regioes.GetPagedAsync(query, ct)).Map(r => r.ToResponse());

    public async Task<RegiaoResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        (await BuscarAsync(id, ct)).ToResponse();

    public async Task<RegiaoResponse> CriarAsync(RegiaoRequest request, CancellationToken ct = default)
    {
        var regiao = new Regiao(request.Nome, request.Municipio, request.Estado,
            request.Latitude, request.Longitude, request.PopulacaoEstimada);
        await regioes.AddAsync(regiao, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return regiao.ToResponse();
    }

    public async Task<RegiaoResponse> AtualizarAsync(Guid id, RegiaoRequest request, CancellationToken ct = default)
    {
        var regiao = await BuscarAsync(id, ct);
        regiao.Atualizar(request.Nome, request.Municipio, request.Estado,
            request.Latitude, request.Longitude, request.PopulacaoEstimada);
        await unitOfWork.SaveChangesAsync(ct);
        return regiao.ToResponse();
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var regiao = await BuscarAsync(id, ct);
        regioes.Remove(regiao);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Regiao> BuscarAsync(Guid id, CancellationToken ct) =>
        await regioes.GetByIdAsync(id, ct) ?? throw new NotFoundException("Região não encontrada.");
}
