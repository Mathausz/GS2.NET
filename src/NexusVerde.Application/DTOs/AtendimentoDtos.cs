using System.Text.Json.Serialization;
using NexusVerde.Application.Hateoas;
using System.ComponentModel.DataAnnotations;
using NexusVerde.Application.Validation;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.DTOs;

public sealed class AtendimentoRequest
{
    [NotEmptyGuid]
    public Guid SolicitacaoId { get; set; }

    /// <summary>Se omitido, usa o ponto sugerido na solicitação.</summary>
    public Guid? PontoAguaId { get; set; }

    [StringLength(1000)]
    public string? Observacao { get; set; }
}

public sealed class AtendimentoStatusRequest
{
    /// <summary>Concluido ou Cancelado.</summary>
    [EnumDataType(typeof(StatusAtendimento))]
    public StatusAtendimento Status { get; set; }

    [StringLength(1000)]
    public string? Observacao { get; set; }
}

public sealed record AtendimentoResponse(Guid Id, Guid SolicitacaoId, Guid GestorId, Guid? PontoAguaId,
    StatusAtendimento Status, string? Observacao, DateTime IniciadoEm, DateTime? FinalizadoEm)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<LinkDto>? Links { get; init; }
}
