using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using System.ComponentModel.DataAnnotations;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

public sealed class OrientacaoRequest
{
    [EnumDataType(typeof(TipoSolicitacao))]
    public TipoSolicitacao Tipo { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 5)]
    public string Texto { get; set; } = string.Empty;

    [Range(0, 1000)]
    public int Ordem { get; set; }
}

public sealed record OrientacaoResponse(Guid Id, TipoSolicitacao Tipo, string Titulo, string Texto, int Ordem)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}
