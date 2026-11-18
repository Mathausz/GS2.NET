using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Application.Queries;
using NexusVerde.Domain.Entities;
using NexusVerde.Infrastructure.Data;

namespace NexusVerde.Infrastructure.Repositories;

public class UsuarioRepository(NexusVerdeDbContext context) : Repository<Usuario>(context), IUsuarioRepository
{
    private static readonly Dictionary<string, Func<IQueryable<Usuario>, bool, IOrderedQueryable<Usuario>>> Campos = new()
    {
        ["nome"] = (q, d) => d ? q.OrderByDescending(x => x.Nome) : q.OrderBy(x => x.Nome),
        ["email"] = (q, d) => d ? q.OrderByDescending(x => x.Email) : q.OrderBy(x => x.Email),
        ["perfil"] = (q, d) => d ? q.OrderByDescending(x => x.Perfil) : q.OrderBy(x => x.Perfil),
    };

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<PagedResult<Usuario>> GetPagedAsync(UsuarioQuery query, CancellationToken ct = default)
    {
        IQueryable<Usuario> q = Set.AsNoTracking();

        if (query.Perfil.HasValue) q = q.Where(x => x.Perfil == query.Perfil.Value);
        if (!string.IsNullOrWhiteSpace(query.Busca))
        {
            var busca = query.Busca.Trim().ToLower();
            q = q.Where(x => x.Nome.ToLower().Contains(busca) || x.Email.Contains(busca));
        }

        return q.Ordenar(query, Campos, x => x.OrderBy(u => u.Nome).ThenBy(u => u.Id)).ToPagedAsync(query, ct);
    }
}
