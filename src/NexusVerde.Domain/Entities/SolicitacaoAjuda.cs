using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Domain.Entities;

/// <summary>Pedido de ajuda feito por um cidadão (falta de água, água suspeita ou acesso negado).</summary>
public class SolicitacaoAjuda : EntityBase
{
    public Guid CidadaoId { get; private set; }
    public Guid RegiaoId { get; private set; }
    public Guid? DesastreId { get; private set; }
    public Guid? PontoSugeridoId { get; private set; }
    public TipoSolicitacao Tipo { get; private set; }
    public string Descricao { get; private set; } = null!;
    public int QuantidadePessoas { get; private set; }
    public int HorasSemAgua { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double ScorePrioridade { get; private set; }
    public NivelPrioridade NivelPrioridade { get; private set; }
    public OrgaoResponsavel OrgaoDestino { get; private set; }
    public StatusSolicitacao Status { get; private set; } = StatusSolicitacao.Aberta;

    protected SolicitacaoAjuda() { }

    public SolicitacaoAjuda(Guid cidadaoId, Guid regiaoId, TipoSolicitacao tipo, string descricao,
        int quantidadePessoas, int horasSemAgua, double latitude, double longitude,
        double scorePrioridade, NivelPrioridade nivelPrioridade, OrgaoResponsavel orgaoDestino,
        Guid? desastreId, Guid? pontoSugeridoId)
    {
        Guard.Coordenadas(latitude, longitude);
        Guard.Faixa(quantidadePessoas, 1, 100_000, "Quantidade de pessoas");
        Guard.Faixa(horasSemAgua, 0, 2_000, "Horas sem água");

        CidadaoId = cidadaoId;
        RegiaoId = regiaoId;
        Tipo = tipo;
        Descricao = Guard.NotEmpty(descricao, "Descrição", 1000);
        QuantidadePessoas = quantidadePessoas;
        HorasSemAgua = horasSemAgua;
        Latitude = latitude;
        Longitude = longitude;
        ScorePrioridade = scorePrioridade;
        NivelPrioridade = nivelPrioridade;
        OrgaoDestino = orgaoDestino;
        DesastreId = desastreId;
        PontoSugeridoId = pontoSugeridoId;
    }

    public void IniciarAtendimento()
    {
        if (Status is StatusSolicitacao.Atendida or StatusSolicitacao.Cancelada)
            throw new DomainException($"Solicitação {Status} não pode receber atendimento.");

        Status = StatusSolicitacao.EmAtendimento;
        MarcarAtualizado();
    }

    public void MarcarAtendida()
    {
        if (Status == StatusSolicitacao.Cancelada)
            throw new DomainException("Solicitação cancelada não pode ser marcada como atendida.");

        Status = StatusSolicitacao.Atendida;
        MarcarAtualizado();
    }

    /// <summary>Usado quando o atendimento em andamento é cancelado pelo gestor.</summary>
    public void RetornarParaAberta()
    {
        if (Status != StatusSolicitacao.EmAtendimento)
            throw new DomainException("Apenas solicitações em atendimento podem voltar para aberta.");

        Status = StatusSolicitacao.Aberta;
        MarcarAtualizado();
    }

    public void Cancelar()
    {
        if (Status != StatusSolicitacao.Aberta)
            throw new DomainException("Somente solicitações abertas podem ser canceladas.");

        Status = StatusSolicitacao.Cancelada;
        MarcarAtualizado();
    }
}
