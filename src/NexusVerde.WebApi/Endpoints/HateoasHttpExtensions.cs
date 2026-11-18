using System.Security.Claims;
using NexusVerde.Api.Security;
using NexusVerde.Application.Common;
using NexusVerde.Application.Exceptions;
using NexusVerde.Application.Hateoas;

namespace NexusVerde.Api.Endpoints;

/// <summary>Ponte entre HTTP e a lógica pura de HATEOAS (<see cref="ResourceLinks"/>).</summary>
internal static class HateoasHttpExtensions
{
    /// <summary>Contexto do usuário autenticado, ou null para chamadas anônimas (ex.: orientações públicas).</summary>
    public static UsuarioContexto? ContextoOuNull(this ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return null;

        try
        {
            return user.ToContexto();
        }
        catch (UnauthorizedException)
        {
            return null;
        }
    }

    /// <summary>Preenche os links self/first/prev/next/last preservando os filtros da consulta.</summary>
    public static PagedResult<T> ComLinksDePagina<T>(this PagedResult<T> pagina, HttpRequest request)
    {
        var consulta = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        pagina.Links = ResourceLinks.Pagina(request.Path.Value ?? string.Empty, consulta,
            pagina.Page, pagina.PageSize, pagina.TotalPages);
        return pagina;
    }
}
