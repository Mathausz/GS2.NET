using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Mapping;
using NexusVerde.Application.Queries;
using NexusVerde.Application.Services.Interfaces;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Services.Implementations;

public class DesastreService(
    IDesastreRepository desastres,
    IRegiaoRepository regioes,
    IUnitOfWork unitOfWork) : IDesastreService
{
    public async Task<PagedResult<DesastreResponse>> ListarAsync(DesastreQuery query, CancellationToken ct = default) =>
        (await desastres.GetPagedAsync(query, ct)).Map(d => d.ToResponse());

    public async Task<DesastreResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        (await BuscarAsync(id, ct)).ToResponse();

    public async Task<DesastreResponse> CriarAsync(DesastreRequest request, CancellationToken ct = default)
    {
        _ = await regioes.GetByIdAsync(request.RegiaoId, ct) ?? throw new NotFoundException("Região não encontrada.");

        var desastre = new Desastre(request.RegiaoId, request.Tipo, request.Gravidade, request.Descricao);
        await desastres.AddAsync(desastre, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await RecalcularRiscoAsync(desastre.RegiaoId, ct);
        return desastre.ToResponse();
    }

    public async Task<DesastreResponse> AtualizarAsync(Guid id, DesastreUpdateRequest request, CancellationToken ct = default)
    {
        var desastre = await BuscarAsync(id, ct);
        desastre.Atualizar(request.Tipo, request.Gravidade, request.Descricao, request.Status);
        await unitOfWork.SaveChangesAsync(ct);
        await RecalcularRiscoAsync(desastre.RegiaoId, ct);
        return desastre.ToResponse();
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var desastre = await BuscarAsync(id, ct);
        desastres.Remove(desastre);
        await unitOfWork.SaveChangesAsync(ct);
        await RecalcularRiscoAsync(desastre.RegiaoId, ct);
    }

    /// <summary>O risco da região acompanha o desastre ativo mais grave.</summary>
    private async Task RecalcularRiscoAsync(Guid regiaoId, CancellationToken ct)
    {
        var regiao = await regioes.GetByIdAsync(regiaoId, ct);
        if (regiao is null) return;

        var maisGrave = await desastres.GetMaisGraveAtivoAsync(regiaoId, ct);
        regiao.DefinirRisco(RiscoCalculator.Classificar(maisGrave?.Gravidade ?? 0));
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Desastre> BuscarAsync(Guid id, CancellationToken ct) =>
        await desastres.GetByIdAsync(id, ct) ?? throw new NotFoundException("Desastre não encontrado.");
}
