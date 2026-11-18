using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusAtendimento
{
    EmAndamento = 1,
    Concluido = 2,
    Cancelado = 3,
}
