using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

/// <summary>Anotação de um gestor na linha do tempo da solicitação.</summary>
public sealed class NotaRequest
{
    [Required, StringLength(1000, MinimumLength = 3)]
    public string Texto { get; set; } = string.Empty;
}

public sealed record TimelineEventoResponse(string Id, Guid SolicitacaoId, TipoEventoTimeline Tipo, string Descricao,
    Guid? AutorId, PerfilUsuario? AutorPerfil, OrgaoResponsavel? Orgao,
    IReadOnlyDictionary<string, string> Dados, DateTime OcorridoEm)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}
