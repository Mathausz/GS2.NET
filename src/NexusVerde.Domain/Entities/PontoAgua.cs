using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Domain.Entities;

/// <summary>Local seguro de distribuição/abastecimento de água potável.</summary>
public class PontoAgua : EntityBase
{
    public Guid RegiaoId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Endereco { get; private set; } = null!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int CapacidadeLitrosDia { get; private set; }
    public StatusPontoAgua Status { get; private set; } = StatusPontoAgua.Ativo;
    public string? Descricao { get; private set; }

    protected PontoAgua() { }

    public PontoAgua(Guid regiaoId, string nome, string endereco, double latitude, double longitude,
        int capacidadeLitrosDia, string? descricao = null)
    {
        RegiaoId = regiaoId;
        Atualizar(nome, endereco, latitude, longitude, capacidadeLitrosDia, descricao);
        AtualizadoEm = null;
    }

    /// <summary>Somente pontos ativos podem ser indicados ou usados em um atendimento.</summary>
    public bool PodeAtender => Status == StatusPontoAgua.Ativo;

    public void Atualizar(string nome, string endereco, double latitude, double longitude,
        int capacidadeLitrosDia, string? descricao)
    {
        Guard.Coordenadas(latitude, longitude);
        Guard.Faixa(capacidadeLitrosDia, 0, 100_000_000, "Capacidade (litros/dia)");

        Nome = Guard.NotEmpty(nome, "Nome", 120);
        Endereco = Guard.NotEmpty(endereco, "Endereço", 200);
        Latitude = latitude;
        Longitude = longitude;
        CapacidadeLitrosDia = capacidadeLitrosDia;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        MarcarAtualizado();
    }

    public void AlterarStatus(StatusPontoAgua status)
    {
        Status = status;
        MarcarAtualizado();
    }
}
