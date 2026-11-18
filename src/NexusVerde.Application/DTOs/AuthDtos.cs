using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using System.ComponentModel.DataAnnotations;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

/// <summary>Cadastro público de cidadão.</summary>
public sealed class RegistroRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter de 8 a 100 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Senha { get; set; } = string.Empty;
}

public sealed record UsuarioResponse(Guid Id, string Nome, string Email, PerfilUsuario Perfil, OrgaoResponsavel Orgao, bool Ativo)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}

public sealed record LoginResponse(string Token, DateTime ExpiraEm, UsuarioResponse Usuario);

/// <summary>Criação de usuário por um administrador (inclusive gestores).</summary>
public sealed class UsuarioCreateRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Senha { get; set; } = string.Empty;

    [EnumDataType(typeof(PerfilUsuario))]
    public PerfilUsuario Perfil { get; set; }

    /// <summary>Obrigatório para Gestor (ex.: DefesaCivil, Prefeitura, Bombeiros, Policia, Ong).</summary>
    [EnumDataType(typeof(OrgaoResponsavel))]
    public OrgaoResponsavel Orgao { get; set; } = OrgaoResponsavel.Nenhum;
}
