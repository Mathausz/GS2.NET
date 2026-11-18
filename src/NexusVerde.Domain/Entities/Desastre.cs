using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Domain.Entities;

/// <summary>Evento (enchente, seca, contaminação) que compromete o abastecimento de uma região.</summary>
public class Desastre : EntityBase
{
    public Guid RegiaoId { get; private set; }
    public TipoDesastre Tipo { get; private set; }
    /// <summary>Gravidade de 1 (leve) a 5 (catastrófica).</summary>
    public int Gravidade { get; private set; }
    public string Descricao { get; private set; } = null!;
    public StatusDesastre Status { get; private set; } = StatusDesastre.Ativo;
    public DateTime IniciadoEm { get; private set; } = DateTime.UtcNow;
    public DateTime? EncerradoEm { get; private set; }

    protected Desastre() { }

    public Desastre(Guid regiaoId, TipoDesastre tipo, int gravidade, string descricao)
    {
        RegiaoId = regiaoId;
        Atualizar(tipo, gravidade, descricao, StatusDesastre.Ativo);
        AtualizadoEm = null;
    }

    public bool EstaAtivo => Status != StatusDesastre.Encerrado;

    public void Atualizar(TipoDesastre tipo, int gravidade, string descricao, StatusDesastre status)
    {
        Guard.Faixa(gravidade, 1, 5, "Gravidade");
        if (Status == StatusDesastre.Encerrado && status != StatusDesastre.Encerrado)
            throw new DomainException("Um desastre encerrado não pode ser reaberto.");

        Tipo = tipo;
        Gravidade = gravidade;
        Descricao = Guard.NotEmpty(descricao, "Descrição", 1000);
        Status = status;
        EncerradoEm = status == StatusDesastre.Encerrado ? (EncerradoEm ?? DateTime.UtcNow) : null;
        MarcarAtualizado();
    }
}
