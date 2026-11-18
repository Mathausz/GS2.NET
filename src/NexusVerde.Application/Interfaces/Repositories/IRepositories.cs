using NexusVerde.Application.Common;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Interfaces.Repositories;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<PagedResult<Usuario>> GetPagedAsync(UsuarioQuery query, CancellationToken ct = default);
}

public interface IRegiaoRepository : IRepository<Regiao>
{
    Task<PagedResult<Regiao>> GetPagedAsync(RegiaoQuery query, CancellationToken ct = default);
}

public interface IDesastreRepository : IRepository<Desastre>
{
    Task<PagedResult<Desastre>> GetPagedAsync(DesastreQuery query, CancellationToken ct = default);
    /// <summary>Desastre ativo de maior gravidade da região (null se não houver).</summary>
    Task<Desastre?> GetMaisGraveAtivoAsync(Guid regiaoId, CancellationToken ct = default);
}

public interface IPontoAguaRepository : IRepository<PontoAgua>
{
    Task<PagedResult<PontoAgua>> GetPagedAsync(PontoAguaQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<PontoAgua>> ListarAtivosAsync(CancellationToken ct = default);
}

public interface ISolicitacaoRepository : IRepository<SolicitacaoAjuda>
{
    /// <param name="cidadaoId">Restringe às solicitações do cidadão.</param>
    /// <param name="orgaoRestrito">Restringe ao órgão do gestor.</param>
    /// <param name="filaPadrao">Sem filtro de status, retorna apenas abertas e em atendimento.</param>
    Task<PagedResult<SolicitacaoAjuda>> GetPagedAsync(SolicitacaoQuery query, Guid? cidadaoId,
        OrgaoResponsavel? orgaoRestrito, bool filaPadrao, CancellationToken ct = default);
}

public interface IAtendimentoRepository : IRepository<Atendimento>
{
    Task<PagedResult<Atendimento>> GetPagedAsync(AtendimentoQuery query, Guid? gestorId, CancellationToken ct = default);
}

public interface IOrientacaoRepository : IRepository<Orientacao>
{
    Task<PagedResult<Orientacao>> GetPagedAsync(OrientacaoQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<Orientacao>> ListarPorTipoAsync(TipoSolicitacao tipo, CancellationToken ct = default);
}
