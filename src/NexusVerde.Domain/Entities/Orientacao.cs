using NexusVerde.Domain.Commons;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Domain.Entities;

/// <summary>
/// Instrução de segurança exibida ao cidadão conforme o tipo da situação
/// (ex.: o que fazer ao suspeitar que uma fonte de água está contaminada).
/// </summary>
public class Orientacao : EntityBase
{
    public TipoSolicitacao Tipo { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Texto { get; private set; } = null!;
    public int Ordem { get; private set; }

    protected Orientacao() { }

    public Orientacao(TipoSolicitacao tipo, string titulo, string texto, int ordem)
    {
        Atualizar(tipo, titulo, texto, ordem);
        AtualizadoEm = null;
    }

    public void Atualizar(TipoSolicitacao tipo, string titulo, string texto, int ordem)
    {
        Guard.Faixa(ordem, 0, 1000, "Ordem");
        Tipo = tipo;
        Titulo = Guard.NotEmpty(titulo, "Título", 150);
        Texto = Guard.NotEmpty(texto, "Texto", 2000);
        Ordem = ordem;
        MarcarAtualizado();
    }
}
