using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using NexusVerde.Application.Validation;
using System.ComponentModel.DataAnnotations;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

public sealed class RegiaoRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string Municipio { get; set; } = string.Empty;

    /// <summary>Sigla da UF (ex.: RS).</summary>
    [Required, StringLength(2, MinimumLength = 2, ErrorMessage = "Estado deve ter 2 letras (UF).")]
    public string Estado { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 100_000_000)]
    public int PopulacaoEstimada { get; set; }
}

public sealed record RegiaoResponse(Guid Id, string Nome, string Municipio, string Estado, double Latitude,
    double Longitude, int PopulacaoEstimada, NivelRisco NivelRisco, DateTime CriadoEm)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}

public sealed class DesastreRequest
{
    [NotEmptyGuid]
    public Guid RegiaoId { get; set; }

    [EnumDataType(typeof(TipoDesastre))]
    public TipoDesastre Tipo { get; set; }

    /// <summary>1 (leve) a 5 (catastrófica).</summary>
    [Range(1, 5)]
    public int Gravidade { get; set; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Descricao { get; set; } = string.Empty;
}

public sealed class DesastreUpdateRequest
{
    [EnumDataType(typeof(TipoDesastre))]
    public TipoDesastre Tipo { get; set; }

    [Range(1, 5)]
    public int Gravidade { get; set; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Descricao { get; set; } = string.Empty;

    [EnumDataType(typeof(StatusDesastre))]
    public StatusDesastre Status { get; set; }
}

public sealed record DesastreResponse(Guid Id, Guid RegiaoId, TipoDesastre Tipo, int Gravidade, string Descricao,
    StatusDesastre Status, DateTime IniciadoEm, DateTime? EncerradoEm)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}
