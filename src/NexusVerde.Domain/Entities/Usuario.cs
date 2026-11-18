using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Domain.Entities;

/// <summary>Usuário da plataforma: cidadão, gestor (órgão responsável) ou administrador.</summary>
public class Usuario : EntityBase
{
    public string Nome { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public PerfilUsuario Perfil { get; private set; }
    public OrgaoResponsavel Orgao { get; private set; }
    public bool Ativo { get; private set; } = true;

    protected Usuario() { }

    public Usuario(string nome, string email, string senhaHash, PerfilUsuario perfil, OrgaoResponsavel orgao = OrgaoResponsavel.Nenhum)
    {
        Nome = Guard.NotEmpty(nome, "Nome", 120);
        Email = Guard.NotEmpty(email, "E-mail", 160).ToLowerInvariant();
        SenhaHash = Guard.NotEmpty(senhaHash, "Senha", 500);
        Perfil = perfil;

        if (perfil == PerfilUsuario.Gestor && orgao == OrgaoResponsavel.Nenhum)
            throw new DomainException("Gestor precisa estar vinculado a um órgão responsável.");

        Orgao = perfil == PerfilUsuario.Gestor ? orgao : OrgaoResponsavel.Nenhum;
    }

    public void Desativar()
    {
        Ativo = false;
        MarcarAtualizado();
    }
}
