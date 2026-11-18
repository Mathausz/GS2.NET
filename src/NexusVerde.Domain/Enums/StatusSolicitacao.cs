using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusSolicitacao
{
    Aberta = 1,
    EmAtendimento = 2,
    Atendida = 3,
    Cancelada = 4,
}
