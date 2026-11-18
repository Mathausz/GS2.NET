namespace NexusVerde.Domain.Commons;

/// <summary>Base de todas as entidades: identificador Guid e datas de auditoria.</summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    public void MarcarAtualizado() => AtualizadoEm = DateTime.UtcNow;
}
