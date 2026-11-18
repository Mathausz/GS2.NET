using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Domain.Entities;

/// <summary>Comunidade/região que pode ser afetada por uma crise hídrica.</summary>
public class Regiao : EntityBase
{
    public string Nome { get; private set; } = null!;
    public string Municipio { get; private set; } = null!;
    public string Estado { get; private set; } = null!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int PopulacaoEstimada { get; private set; }
    public NivelRisco NivelRisco { get; private set; } = NivelRisco.Baixo;

    protected Regiao() { }

    public Regiao(string nome, string municipio, string estado, double latitude, double longitude, int populacaoEstimada)
    {
        Atualizar(nome, municipio, estado, latitude, longitude, populacaoEstimada);
        AtualizadoEm = null;
    }

    public void Atualizar(string nome, string municipio, string estado, double latitude, double longitude, int populacaoEstimada)
    {
        Guard.Coordenadas(latitude, longitude);
        Guard.Faixa(populacaoEstimada, 0, 100_000_000, "População estimada");

        Nome = Guard.NotEmpty(nome, "Nome", 120);
        Municipio = Guard.NotEmpty(municipio, "Município", 120);
        Estado = Guard.NotEmpty(estado, "Estado", 2).ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        PopulacaoEstimada = populacaoEstimada;
        MarcarAtualizado();
    }

    public void DefinirRisco(NivelRisco nivel)
    {
        NivelRisco = nivel;
        MarcarAtualizado();
    }
}
