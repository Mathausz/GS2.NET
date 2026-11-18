using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Domain.Commons;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class Repository<T>(NexusVerdeDbContext context) : IRepository<T> where T : EntityBase
{
    protected NexusVerdeDbContext Context { get; } = context;
    protected DbSet<T> Set => Context.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public void Remove(T entity) => Set.Remove(entity);
}

public class UnitOfWork(NexusVerdeDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
