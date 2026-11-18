using System.Security.Cryptography;

namespace NexusVerde.Api.Security;

/// <summary>
/// Chave JWT temporária usada <b>somente em Development</b> quando <c>Jwt:Key</c> não foi configurada.
/// É aleatória e gerada a cada subida (tokens antigos deixam de valer ao reiniciar). Nenhuma chave fica no repositório;
/// fora de Development a chave continua obrigatória.
/// </summary>
public static class ChaveJwtDesenvolvimento
{
    private static readonly Lazy<string> Chave = new(() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));

    /// <summary>Indica se a chave temporária foi usada nesta execução.</summary>
    public static bool Utilizada { get; private set; }

    public static string Obter()
    {
        Utilizada = true;
        return Chave.Value;
    }
}
