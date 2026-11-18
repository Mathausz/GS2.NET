using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Domain.Entities;

/// <summary>Ação de um gestor para atender uma solicitação, opcionalmente usando um ponto de água.</summary>
public class Atendimento : EntityBase
{
    public Guid SolicitacaoId { get; private set; }
    public Guid GestorId { get; private set; }
    public Guid? PontoAguaId { get; private set; }
    public StatusAtendimento Status { get; private set; } = StatusAtendimento.EmAndamento;
    public string? Observacao { get; private set; }
    public DateTime IniciadoEm { get; private set; } = DateTime.UtcNow;
    public DateTime? FinalizadoEm { get; private set; }

    protected Atendimento() { }

    public Atendimento(Guid solicitacaoId, Guid gestorId, Guid? pontoAguaId, string? observacao)
    {
        SolicitacaoId = solicitacaoId;
        GestorId = gestorId;
        PontoAguaId = pontoAguaId;
        Observacao = Normalizar(observacao);
    }

    public void Concluir(string? observacao) => Finalizar(StatusAtendimento.Concluido, observacao);

    public void Cancelar(string? observacao) => Finalizar(StatusAtendimento.Cancelado, observacao);

    private void Finalizar(StatusAtendimento status, string? observacao)
    {
        if (Status != StatusAtendimento.EmAndamento)
            throw new DomainException($"Atendimento já está {Status}.");

        Status = status;
        FinalizadoEm = DateTime.UtcNow;
        Observacao = Normalizar(observacao) ?? Observacao;
        MarcarAtualizado();
    }

    private static string? Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var t = texto.Trim();
        return t.Length > 1000 ? t[..1000] : t;
    }
}
