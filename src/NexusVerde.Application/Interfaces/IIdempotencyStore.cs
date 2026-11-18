using NexusVerde.Domain.Entities;

namespace NexusVerde.Application.Interfaces;

public interface IIdempotencyStore
{
    Task<RegistroIdempotencia?> BuscarAsync(string chave, Guid usuarioId, string rota, CancellationToken ct = default);
    Task SalvarAsync(RegistroIdempotencia registro, CancellationToken ct = default);

    /// <summary>Remove um registro vencido (a chave pode então ser reutilizada).</summary>
    Task RemoverAsync(RegistroIdempotencia registro, CancellationToken ct = default);
}
