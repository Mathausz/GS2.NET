using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NivelPrioridade
{
    Baixa = 1,
    Media = 2,
    Alta = 3,
    Critica = 4,
}
