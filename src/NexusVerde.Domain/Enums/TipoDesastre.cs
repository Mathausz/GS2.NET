using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoDesastre
{
    Enchente = 1,
    Seca = 2,
    Contaminacao = 3,
    Outro = 4,
}
