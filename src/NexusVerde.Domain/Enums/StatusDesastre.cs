using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusDesastre
{
    Ativo = 1,
    Controlado = 2,
    Encerrado = 3,
}
