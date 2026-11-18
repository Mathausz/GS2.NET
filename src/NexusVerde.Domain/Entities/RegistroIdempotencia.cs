using NexusVerde.Domain.Commons;

namespace NexusVerde.Domain.Entities;

/// <summary>
/// Resposta guardada de uma requisição idempotente (header Idempotency-Key),
/// permitindo devolver o mesmo resultado em repetições sem duplicar efeitos.
/// </summary>
public class RegistroIdempotencia : EntityBase
{
    public string Chave { get; set; } = null!;
    public Guid UsuarioId { get; set; }
    public string Rota { get; set; } = null!;
    public string HashCorpo { get; set; } = null!;
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string? CorpoResposta { get; set; }
}
