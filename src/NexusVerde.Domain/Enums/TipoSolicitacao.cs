using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoSolicitacao
{
    SemAgua = 1,
    AguaSuspeita = 2,
    AcessoNegado = 3,
}
