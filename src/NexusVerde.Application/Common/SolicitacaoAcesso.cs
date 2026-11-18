using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Common;

/// <summary>Regra única de acesso a uma solicitação (e à sua linha do tempo).</summary>
public static class SolicitacaoAcesso
{
    /// <summary>Cidadão só acessa as próprias; gestores, as do seu órgão (Defesa Civil e Admin, todas).</summary>
    public static bool Pode(UsuarioContexto usuario, SolicitacaoAjuda solicitacao) =>
        usuario.Perfil == PerfilUsuario.Cidadao
            ? solicitacao.CidadaoId == usuario.Id
            : usuario.PodeVerOrgao(solicitacao.OrgaoDestino);
}
