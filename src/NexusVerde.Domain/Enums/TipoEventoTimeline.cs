using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoEventoTimeline
{
    Criada = 1,
    Cancelada = 2,
    AtendimentoIniciado = 3,
    AtendimentoConcluido = 4,
    AtendimentoCancelado = 5,
    NotaAdicionada = 6,
}
