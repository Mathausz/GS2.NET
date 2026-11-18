using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Services.Implementations;

public class OrientacaoService(IOrientacaoRepository orientacoes, IUnitOfWork unitOfWork) : IOrientacaoService
{
    public async Task<PagedResult<OrientacaoResponse>> ListarAsync(OrientacaoQuery query, CancellationToken ct = default) =>
        (await orientacoes.GetPagedAsync(query, ct)).Map(o => o.ToResponse());

    public async Task<OrientacaoResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        (await BuscarAsync(id, ct)).ToResponse();

    public async Task<OrientacaoResponse> CriarAsync(OrientacaoRequest request, CancellationToken ct = default)
    {
        var orientacao = new Orientacao(request.Tipo, request.Titulo, request.Texto, request.Ordem);
        await orientacoes.AddAsync(orientacao, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return orientacao.ToResponse();
    }

    public async Task<OrientacaoResponse> AtualizarAsync(Guid id, OrientacaoRequest request, CancellationToken ct = default)
    {
        var orientacao = await BuscarAsync(id, ct);
        orientacao.Atualizar(request.Tipo, request.Titulo, request.Texto, request.Ordem);
        await unitOfWork.SaveChangesAsync(ct);
        return orientacao.ToResponse();
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var orientacao = await BuscarAsync(id, ct);
        orientacoes.Remove(orientacao);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Orientacao> BuscarAsync(Guid id, CancellationToken ct) =>
        await orientacoes.GetByIdAsync(id, ct) ?? throw new NotFoundException("Orientação não encontrada.");
}
