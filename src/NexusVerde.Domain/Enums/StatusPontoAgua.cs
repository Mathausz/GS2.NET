using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusPontoAgua
{
    Ativo = 1,
    Lotado = 2,
    Contaminado = 3,
    Inativo = 4,
}
