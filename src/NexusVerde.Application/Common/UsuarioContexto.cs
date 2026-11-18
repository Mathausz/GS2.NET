using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Common;

/// <summary>Identidade do usuário autenticado, extraída do JWT pela camada de API.</summary>
public sealed record UsuarioContexto(Guid Id, PerfilUsuario Perfil, OrgaoResponsavel Orgao)
{
    public bool EhAdmin => Perfil == PerfilUsuario.Admin;
    public bool EhGestor => Perfil == PerfilUsuario.Gestor;

    /// <summary>
    /// Admin e Defesa Civil (coordenação) enxergam todos os órgãos;
    /// os demais gestores só enxergam o próprio órgão.
    /// </summary>
    public bool PodeVerOrgao(OrgaoResponsavel destino) =>
        EhAdmin || (EhGestor && (Orgao == OrgaoResponsavel.DefesaCivil || Orgao == destino));

    /// <summary>Órgão ao qual a listagem deve ser restringida (null = sem restrição).</summary>
    public OrgaoResponsavel? OrgaoRestrito =>
        EhGestor && Orgao != OrgaoResponsavel.DefesaCivil ? Orgao : null;
}
