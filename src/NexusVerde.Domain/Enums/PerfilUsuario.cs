using System.Text.Json.Serialization;

namespace NexusVerde.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PerfilUsuario
{
    Cidadao = 1,
    Gestor = 2,
    Admin = 3,
}
