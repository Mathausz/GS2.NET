using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using System.ComponentModel.DataAnnotations;
using NexusVerde.Application.Validation;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

public sealed class SolicitacaoRequest
{
    [NotEmptyGuid]
    public Guid RegiaoId { get; set; }

    /// <summary>SemAgua, AguaSuspeita (risco de contaminação) ou AcessoNegado.</summary>
    [EnumDataType(typeof(TipoSolicitacao))]
    public TipoSolicitacao Tipo { get; set; }

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Descricao { get; set; } = string.Empty;

    [Range(1, 100_000)]
    public int QuantidadePessoas { get; set; } = 1;

    [Range(0, 2000)]
    public int HorasSemAgua { get; set; }

    /// <summary>Opcional: se omitida, usa o centro da região.</summary>
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }
}

public sealed record SolicitacaoResponse(Guid Id, Guid CidadaoId, Guid RegiaoId, Guid? DesastreId, Guid? PontoSugeridoId,
    TipoSolicitacao Tipo, string Descricao, int QuantidadePessoas, int HorasSemAgua, double Latitude, double Longitude,
    double ScorePrioridade, NivelPrioridade NivelPrioridade, OrgaoResponsavel OrgaoDestino,
    StatusSolicitacao Status, DateTime CriadoEm)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}

/// <summary>Resposta da criação: solicitação, ponto de água mais próximo e instruções de segurança.</summary>
public sealed record SolicitacaoCriadaResponse(SolicitacaoResponse Solicitacao,
    PontoAguaProximoResponse? PontoMaisProximo, IReadOnlyList<OrientacaoResponse> Orientacoes);
