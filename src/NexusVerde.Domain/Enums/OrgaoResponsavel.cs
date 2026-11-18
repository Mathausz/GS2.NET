using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrgaoResponsavel
{
    Nenhum = 1,
    DefesaCivil = 2,
    Prefeitura = 3,
    Bombeiros = 4,
    Policia = 5,
    Ong = 6,
}
