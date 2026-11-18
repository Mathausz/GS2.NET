using Microsoft.EntityFrameworkCore;
using NexusVerde.Application.Common;

namespace NexusVerde.Infrastructure.Repositories;

internal static class QueryableExtensions
{
    /// <summary>Aplica contagem total + Skip/Take e devolve a página.</summary>
    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query, QueryParameters parameters, CancellationToken ct)
    {
        var page = parameters.PaginaEfetiva();
        var pageSize = parameters.TamanhoEfetivo();

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }

    /// <summary>
    /// Ordenação segura por whitelist: nomes de campo desconhecidos são ignorados
    /// e a ordenação padrão é usada.
    /// </summary>
    public static IOrderedQueryable<T> Ordenar<T>(
        this IQueryable<T> query,
        QueryParameters parameters,
        IReadOnlyDictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> campos,
        Func<IQueryable<T>, IOrderedQueryable<T>> padrao)
    {
        var chave = parameters.SortBy?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(chave) && campos.TryGetValue(chave, out var ordenar))
            return ordenar(query, parameters.Decrescente());

        return padrao(query);
    }
}
