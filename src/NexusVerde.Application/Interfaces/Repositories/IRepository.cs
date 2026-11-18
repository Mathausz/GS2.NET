using NexusVerde.Domain.Commons;

namespace NexusVerde.Application.Interfaces.Repositories;

public interface IRepository<T> where T : EntityBase
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
}

/// <summary>Confirma as alterações pendentes de todos os repositórios em uma única transação.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
