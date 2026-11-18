using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NivelRisco
{
    Baixo = 1,
    Medio = 2,
    Alto = 3,
    Critico = 4,
}
