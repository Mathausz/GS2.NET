using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using System.ComponentModel.DataAnnotations;
using NexusVerde.Application.Validation;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

public sealed class PontoAguaRequest
{
    [NotEmptyGuid]
    public Guid RegiaoId { get; set; }

    [Required, StringLength(120, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    public string Endereco { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 100_000_000)]
    public int CapacidadeLitrosDia { get; set; }

    [StringLength(500)]
    public string? Descricao { get; set; }
}

public sealed class PontoAguaStatusRequest
{
    [EnumDataType(typeof(StatusPontoAgua))]
    public StatusPontoAgua Status { get; set; }
}

public sealed record PontoAguaResponse(Guid Id, Guid RegiaoId, string Nome, string Endereco, double Latitude,
    double Longitude, int CapacidadeLitrosDia, StatusPontoAgua Status, string? Descricao)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}

public sealed record PontoAguaProximoResponse(PontoAguaResponse Ponto, double DistanciaKm);
