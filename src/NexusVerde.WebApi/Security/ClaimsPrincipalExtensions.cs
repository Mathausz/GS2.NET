using System.Security.Claims;
using NexusVerde.Application.Common;
using NexusVerde.Application.Exceptions;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Api.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Converte as claims do JWT (sub, role, orgao) no contexto usado pelos serviços.</summary>
    public static UsuarioContexto ToContexto(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        var role = user.FindFirst("role")?.Value;

        if (!Guid.TryParse(sub, out var id) || !Enum.TryParse<PerfilUsuario>(role, out var perfil))
            throw new UnauthorizedException("Token inválido.");

        Enum.TryParse<OrgaoResponsavel>(user.FindFirst("orgao")?.Value, out var orgao);
        return new UsuarioContexto(id, perfil, orgao == 0 ? OrgaoResponsavel.Nenhum : orgao);
    }
}
